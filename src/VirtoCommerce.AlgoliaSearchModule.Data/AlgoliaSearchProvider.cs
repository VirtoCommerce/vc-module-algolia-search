using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Algolia.Search.Clients;
using Algolia.Search.Models.Common;
using Algolia.Search.Models.Settings;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.Settings;
using VirtoCommerce.SearchModule.Core.Exceptions;
using VirtoCommerce.SearchModule.Core.Model;
using VirtoCommerce.SearchModule.Core.Services;
using SearchRequest = VirtoCommerce.SearchModule.Core.Model.SearchRequest;

/// <summary>
/// Based on the document from https://www.algolia.com/doc/guides/getting-started/quick-start/tutorials/quick-start-with-the-api-client/csharp/?client=csharp
/// </summary>

namespace VirtoCommerce.AlgoliaSearchModule.Data
{
    public class AlgoliaSearchProvider : ISearchProvider
    {
        private readonly AlgoliaSearchOptions _algoliaSearchOptions;
        private readonly SearchOptions _searchOptions;
        private readonly ISettingsManager _settingsManager;

        public AlgoliaSearchProvider(IOptions<AlgoliaSearchOptions> algoliaSearchOptions, IOptions<SearchOptions> searchOptions, ISettingsManager settingsManager)
        {
            if (algoliaSearchOptions == null)
                throw new ArgumentNullException(nameof(algoliaSearchOptions));

            if (searchOptions == null)
                throw new ArgumentNullException(nameof(searchOptions));

            _algoliaSearchOptions = algoliaSearchOptions.Value;
            _searchOptions = searchOptions.Value;

            _settingsManager = settingsManager ?? throw new ArgumentNullException(nameof(settingsManager));
        }

        private SearchClient _client;
        protected SearchClient Client => _client ??= CreateSearchServiceClient();

        public virtual async Task DeleteIndexAsync(string documentType)
        {
            if (string.IsNullOrEmpty(documentType))
                throw new ArgumentNullException(nameof(documentType));

            try
            {
                var indexName = GetIndexName(documentType);

                if (await IndexExistsAsync(indexName))
                {
                    var index = Client.InitIndex(indexName);
                    await index.DeleteAsync();
                }
            }
            catch (Exception ex)
            {
                ThrowException("Failed to delete index", ex);
            }
        }

        public virtual async Task<IndexingResult> IndexAsync(string documentType, IList<IndexDocument> documents)
        {
            var indexName = GetIndexName(documentType);
            var providerDocuments = documents.Select(document => ConvertToProviderDocument(document, documentType)).ToList();

            //var indexExists = await IndexExistsAsync(documentType);
            var index = Client.InitIndex(indexName);

            // get current setting, so we can update them with new fields if needed
            var settings = await index.ExistsAsync() ? await index.GetSettingsAsync() : new IndexSettings();
            var settingHasChanges = false;

            // define searchable attributes
            foreach(var document in documents)
            {
                foreach (var field in document.Fields.OrderBy(f => f.Name))
                {
                    var fieldName = AlgoliaSearchHelper.ToAlgoliaFieldName(field.Name);
                    if (field.IsSearchable)
                    {
                        if (settings.SearchableAttributes == null)
                        {
                            settings.SearchableAttributes = new List<string>();
                        }

                        if (!settings.SearchableAttributes.Contains(fieldName))
                        {
                            settings.SearchableAttributes.Add(fieldName);
                            settingHasChanges = true;
                        }
                    }

                    if (field.IsFilterable)
                    {
                        if (settings.AttributesForFaceting == null)
                        {
                            settings.AttributesForFaceting = new List<string>();
                        }

                        if (!settings.AttributesForFaceting.Contains(fieldName))
                        {
                            settings.AttributesForFaceting.Add(fieldName);
                            settingHasChanges = true;
                        }
                    }

                    if (field.IsRetrievable)
                    {
                        if (settings.AttributesToRetrieve == null)
                        {
                            settings.AttributesToRetrieve = new List<string>();
                        }

                        if (!settings.AttributesToRetrieve.Contains(fieldName))
                        {
                            settings.AttributesToRetrieve.Add(fieldName);
                            settingHasChanges = true;
                        }
                    }
                }
            }

            // set replicas
            var existingReplicas = settings.Replicas;

            if (existingReplicas == null)
                existingReplicas = new List<string>();

            var replicaSettings = GetSortReplicas(documentType);
            if (replicaSettings != null && replicaSettings.Length > 0)
            {
                var replicaNames = replicaSettings.Select(x => AlgoliaSearchHelper.ToAlgoliaReplicaName(indexName, x)).ToList();

                if (!Enumerable.SequenceEqual(existingReplicas, replicaNames))
                {
                    settingHasChanges = true;

                    settings.Replicas = existingReplicas.Union(replicaNames).ToList();

                    // set sorting field for each replica
                    foreach (var replica in replicaSettings)
                    {
                        var replicaName = AlgoliaSearchHelper.ToAlgoliaReplicaName(indexName, replica);
                        var replicaIndex = Client.InitIndex(replicaName);
                        var replicaSetting = new IndexSettings()
                        {
                            CustomRanking =
                            new List<string> { replica.IsDescending ? $"desc({replica.FieldName})" : $"asc({replica.FieldName})" }
                        };
                        await replicaIndex.SetSettingsAsync(replicaSetting);
                    }
                }
            }

            // only update index if there are changes
            if(settingHasChanges)
                await index.SetSettingsAsync(settings, forwardToReplicas: true);

            var response = await index.SaveObjectsAsync(providerDocuments);

            return CreateIndexingResult(response);
        }

        public virtual async Task<IndexingResult> RemoveAsync(string documentType, IList<IndexDocument> documents)
        {
            IndexingResult result;

            try
            {
                var indexName = GetIndexName(documentType);
                var index = Client.InitIndex(indexName);

                var ids = documents.Select(d => d.Id);
                var response = await index.DeleteObjectsAsync(ids.ToArray());
                result = CreateIndexingResult(response);
            }
            catch (Exception ex)
            {
                throw new SearchException(ex.Message, ex);
            }

            return result;
        }

        public virtual async Task<SearchResponse> SearchAsync(string documentType, SearchRequest request)
        {
            var indexName = AlgoliaSearchHelper.ToAlgoliaIndexName(GetIndexName(documentType), request.Sorting);

            try
            {
                var indexClient = Client.InitIndex(indexName);

                var providerQuery = new AlgoliaSearchRequestBuilder().BuildRequest(request, indexName);
                var response = await indexClient.SearchAsync<SearchDocument>(providerQuery);

                var result = response.ToSearchResponse(request);
                return result;
            }
            catch (Exception ex)
            {
                throw new SearchException(ex.Message, ex);
            }
        }

        protected virtual AlgoliaIndexDocument ConvertToProviderDocument(IndexDocument document, string documentType)
        {
            var result = new AlgoliaIndexDocument { ObjectID = document.Id };

            result.Add(AlgoliaSearchHelper.RawKeyFieldName, document.Id);

            foreach (var field in document.Fields.OrderBy(f => f.Name))
            {
                var fieldName = AlgoliaSearchHelper.ToAlgoliaFieldName(field.Name);

                if (result.ContainsKey(fieldName))
                {
                    var newValues = new List<object>();

                    var currentValue = result[fieldName];

                    if (currentValue is object[] currentValues)
                    {
                        newValues.AddRange(currentValues);
                    }
                    else
                    {
                        newValues.Add(currentValue);
                    }

                    newValues.AddRange(field.Values);
                    result[fieldName] = newValues.ToArray();
                }
                else
                {
                    var isCollection = field.IsCollection || field.Values.Count > 1;


                    var point = field.Value as GeoPoint;
                    var value = isCollection ? field.Values : field.Value;


                    // Only support single field geo location
                    if(field.Value is GeoPoint)
                    {
                        value = new { lat = point.Latitude, lng = point.Longitude };
                        fieldName = "_geoloc";
                    }

                    result.Add(fieldName, value);
                }

                // handle special indexationdate field, need to convert it to sortable numeric value
                // https://www.algolia.com/doc/guides/managing-results/refine-results/sorting/how-to/sort-an-index-by-date/
                if(field.Name.Equals("indexationdate", StringComparison.OrdinalIgnoreCase))
                {
                    result.Add("indexationdate_timestamp", DateTimeToUnixTimestamp((DateTime)field.Value));
                }
                
            }

            return result;
        }


        protected virtual IndexingResult CreateIndexingResult(BatchIndexingResponse results)
        {
            var ids = new List<string>();

            foreach(var response in results.Responses)
            {
                ids.AddRange(response.ObjectIDs);
            }
            return new IndexingResult
            {
                Items = ids.Select(r => new IndexingResultItem
                {
                    Id = r,
                    Succeeded = true,
                    ErrorMessage = string.Empty,
                }).ToArray(),
            };
        }

        protected virtual string GetIndexName(string documentType)
        {
            // Use different index for each document type
            return string.Join("-", _searchOptions.Scope, documentType).ToLowerInvariant();
        }

        protected virtual async Task<bool> IndexExistsAsync(string indexName)
        {
            var indexes = await Client.ListIndicesAsync();
            return indexes.Items.Count > 0 && indexes.Items.Exists(x => x.Name.EqualsInvariant(indexName));
        }

        protected virtual void ThrowException(string message, Exception innerException)
        {
            throw new SearchException($"{message}. Search service name: {_algoliaSearchOptions.AppId}, Scope: {_searchOptions.Scope}", innerException);
        }

        protected virtual SearchClient CreateSearchServiceClient()
        {
            var result = new SearchClient(_algoliaSearchOptions.AppId, _algoliaSearchOptions.ApiKey);
            return result;
        }

        protected virtual AlgoliaIndexSortReplica[] GetSortReplicas(string documentType)
        {
            var replicas = _settingsManager.GetValue("VirtoCommerce.Search.AlgoliaSearch.SortReplicas", new[] { "product:name-asc", "product:name-desc", "product:price-asc", "product:price-desc", "indexationdate_timestamp-desc" });

            if (replicas == null)
                return null;

            var sortReplicas = new List<AlgoliaIndexSortReplica>();
            foreach (var replica in replicas)
            {
                var sortReplica = new AlgoliaIndexSortReplica();
                var replicaArray = replica.Split(':');
                var replicaDocumentType = string.Empty;
                var fieldNameWithSort = string.Empty;

                if (replicaArray.Length > 1)
                {
                    replicaDocumentType = replicaArray[0];
                    if (!replicaDocumentType.Equals(documentType, StringComparison.OrdinalIgnoreCase))
                        continue; // skip if type doesn't match

                    fieldNameWithSort = replicaArray[1];
                }
                else
                {
                    fieldNameWithSort = replicaArray[0];
                }

                if (fieldNameWithSort.EndsWith("asc"))
                {
                    sortReplica.IsDescending = false;
                    sortReplica.FieldName = AlgoliaSearchHelper.ToAlgoliaFieldName(fieldNameWithSort.Substring(0, fieldNameWithSort.LastIndexOf("-asc")));
                }
                else
                {
                    sortReplica.IsDescending = true;
                    sortReplica.FieldName = AlgoliaSearchHelper.ToAlgoliaFieldName(fieldNameWithSort.Substring(0, fieldNameWithSort.LastIndexOf("-desc")));
                }

                sortReplicas.Add(sortReplica);
            }

            return sortReplicas.ToArray();
        }

        public static double DateTimeToUnixTimestamp(DateTime dateTime)
        {
            DateTime unixStart = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
            long unixTimeStampInTicks = (dateTime.ToUniversalTime() - unixStart).Ticks;
            return (double)unixTimeStampInTicks / TimeSpan.TicksPerSecond;
        }
    }
}
