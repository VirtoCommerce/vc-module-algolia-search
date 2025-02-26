using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Algolia.Search.Clients;
using Algolia.Search.Models.Search;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using VirtoCommerce.AlgoliaSearchModule.Data.Extensions;
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
        private readonly ILogger<AlgoliaSearchProvider> _logger;

        public AlgoliaSearchProvider(IOptions<AlgoliaSearchOptions> algoliaSearchOptions,
            IOptions<SearchOptions> searchOptions,
            ISettingsManager settingsManager,
            ILogger<AlgoliaSearchProvider> logger)
        {
            ArgumentNullException.ThrowIfNull(algoliaSearchOptions);
            ArgumentNullException.ThrowIfNull(searchOptions);
            ArgumentNullException.ThrowIfNull(settingsManager);
            ArgumentNullException.ThrowIfNull(logger);

            _algoliaSearchOptions = algoliaSearchOptions.Value;
            _searchOptions = searchOptions.Value;
            _settingsManager = settingsManager;
            _logger = logger;
        }

        private SearchClient _client;
        protected SearchClient Client => _client ??= CreateSearchServiceClient();

        public virtual async Task DeleteIndexAsync(string documentType)
        {
            if (string.IsNullOrEmpty(documentType))
            {
                throw new ArgumentNullException(nameof(documentType));
            }

            try
            {
                var indexName = GetIndexName(documentType);

                if (await IndexExistsAsync(indexName))
                {
                    await Client.DeleteIndexAsync(indexName);
                    _logger.LogInformation("Index {IndexName} deleted successfully.", indexName);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete index {DocumentType}.", documentType);
                ThrowException("Failed to delete index", ex);
            }
        }

        public virtual async Task<IndexingResult> IndexAsync(string documentType, IList<IndexDocument> documents)
        {
            var indexName = GetIndexName(documentType);
            var providerDocuments = documents.Select(document => ConvertToProviderDocument(document, documentType)).ToList();

            // get current setting, so we can update them with new fields if needed
            var settings = new IndexSettings();

            if (await Client.IndexExistsAsync(indexName))
            {
                var currentSettings = (await Client.GetSettingsAsync(indexName));

                settings = new IndexSettings
                {
                    Ranking = currentSettings.Ranking,
                    CustomRanking = currentSettings.CustomRanking,
                    SearchableAttributes = currentSettings.SearchableAttributes,
                    AttributesForFaceting = currentSettings.AttributesForFaceting,
                    Replicas = currentSettings.Replicas,
                    TypoTolerance = currentSettings.TypoTolerance,
                    RemoveStopWords = currentSettings.RemoveStopWords,
                    IgnorePlurals = currentSettings.IgnorePlurals,
                };
            }

            var settingHasChanges = false;

            // define searchable attributes
            foreach (var document in documents)
            {
                foreach (var field in document.Fields.OrderBy(f => f.Name))
                {
                    var fieldName = AlgoliaSearchHelper.ToAlgoliaFieldName(field.Name);
                    if (field.IsSearchable)
                    {
                        if (settings.SearchableAttributes == null)
                        {
                            settings.SearchableAttributes = [];
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

            var existingReplicas = settings.Replicas ?? [];

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

                        var replicaSetting = new IndexSettings()
                        {
                            CustomRanking = [replica.IsDescending ? $"desc({replica.FieldName})" : $"asc({replica.FieldName})"]
                        };

                        await Client.SetSettingsAsync(replicaName, replicaSetting);
                    }
                }
            }

            // only update index if there are changes
            if (settingHasChanges)
            {
                await Client.SetSettingsAsync(indexName, settings, forwardToReplicas: true);
            }

            try
            {
                var response = await Client.SaveObjectsAsync(indexName, providerDocuments);
                _logger.LogInformation("Indexed {DocumentCount} documents in index {IndexName}.", documents.Count, indexName);
                return CreateIndexingResult(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while indexing documents in index {IndexName}.", indexName);
                return new IndexingResult
                {
                    Items = providerDocuments.Select(doc => new IndexingResultItem
                    {
                        Id = doc.ObjectID,
                        Succeeded = false,
                        ErrorMessage = ex.Message
                    }).ToArray()
                };
            }
        }

        public virtual async Task<IndexingResult> RemoveAsync(string documentType, IList<IndexDocument> documents)
        {
            IndexingResult result;

            try
            {
                var indexName = GetIndexName(documentType);

                var ids = documents.Select(d => d.Id);
                var response = await Client.DeleteObjectsAsync(indexName, ids.ToArray());
                result = CreateIndexingResult(response);
                _logger.LogInformation("Removed {DocumentCount} documents from index {IndexName}.", documents.Count, indexName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to remove documents from index {DocumentType}.", documentType);
                throw new SearchException(ex.Message, ex);
            }

            return result;
        }

        public virtual async Task<SearchResponse> SearchAsync(string documentType, SearchRequest request)
        {
            var indexName = AlgoliaSearchHelper.ToAlgoliaIndexName(GetIndexName(documentType), request.Sorting);

            try
            {
                if (!await Client.IndexExistsAsync(indexName))
                {
                    // Fall back to default index name if replica index not found
                    _logger.LogWarning("Replica Index {IndexName} not found for document type {DocumentType}.", indexName, documentType);

                    indexName = GetIndexName(documentType);
                }


                var providerQuery = new AlgoliaSearchRequestBuilder().BuildRequest(request, indexName);

                var response = await Client.SearchSingleIndexAsync<SearchDocument>(indexName, new SearchParams(providerQuery));

                var result = response.ToSearchResponse(request);
                _logger.LogInformation("Search completed on index {IndexName} for document type {DocumentType}.", indexName, documentType);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Search failed on index {IndexName} for document type {DocumentType}.", indexName, documentType);
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

                if (result.TryGetValue(fieldName, out var currentValue))
                {
                    var newValues = new List<object>();

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
                    if (field.Value is GeoPoint)
                    {
                        value = new { lat = point.Latitude, lng = point.Longitude };
                        fieldName = "_geoloc";
                    }

                    if (field.ValueType == IndexDocumentFieldValueType.DateTime)
                    {
                        result.Add(fieldName, DateTimeExtension.DateTimeToUnixTimestamp((DateTime)value));
                    }
                    else
                    {
                        result.Add(fieldName, value);
                    }
                }

                // handle special indexationdate field, need to convert it to sortable numeric value
                // https://www.algolia.com/doc/guides/managing-results/refine-results/sorting/how-to/sort-an-index-by-date/
                if (field.Name.Equals("indexationdate", StringComparison.OrdinalIgnoreCase))
                {
                    result.Add("indexationdate_timestamp", DateTimeExtension.DateTimeToUnixTimestamp((DateTime)field.Value));
                }
            }

            return result;
        }

        protected virtual IndexingResult CreateIndexingResult(IList<BatchResponse> results)
        {
            var ids = new List<string>();

            foreach (var response in results)
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
            var result = new SearchClient(new SearchConfig(_algoliaSearchOptions.AppId, _algoliaSearchOptions.ApiKey));
            return result;
        }

        protected virtual AlgoliaIndexSortReplica[] GetSortReplicas(string documentType)
        {
            var replicas = _settingsManager.GetValue<string[]>(Core.ModuleConstants.Settings.Indexing.SortReplicas);

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
                        continue;

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
    }
}
