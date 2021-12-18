using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Algolia.Search.Clients;
using Algolia.Search.Models.Common;
using Microsoft.Extensions.Options;
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
        private readonly AlgoliaSearchOptions _azureSearchOptions;
        private readonly SearchOptions _searchOptions;
        private readonly ISettingsManager _settingsManager;

        public AlgoliaSearchProvider(IOptions<AlgoliaSearchOptions> algoliaSearchOptions, IOptions<SearchOptions> searchOptions, ISettingsManager settingsManager)
        {
            if (algoliaSearchOptions == null)
                throw new ArgumentNullException(nameof(algoliaSearchOptions));

            if (searchOptions == null)
                throw new ArgumentNullException(nameof(searchOptions));

            _azureSearchOptions = algoliaSearchOptions.Value;
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
            var index = Client.InitIndex(indexName);
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
            throw new NotImplementedException();
            //var indexName = GetIndexName(documentType);

            //try
            //{
            //    var availableFields = await GetMappingAsync(indexName);
            //    var indexClient = GetSearchIndexClient(indexName);

            //    var providerRequests = AzureSearchRequestBuilder.BuildRequest(request, indexName, documentType, availableFields);
            //    var providerResponses = await Task.WhenAll(providerRequests.Select(r => indexClient.Documents.SearchAsync(r?.SearchText, r?.SearchParameters)));

            //    // Copy aggregation ID from request to response
            //    var searchResults = providerResponses.Select((response, i) => new AzureSearchResult
            //    {
            //        AggregationId = providerRequests[i].AggregationId,
            //        ProviderResponse = response,
            //    })
            //    .ToArray();

            //    var result = searchResults.ToSearchResponse(request, documentType);
            //    return result;
            //}
            //catch (Exception ex)
            //{
            //    throw new SearchException(ex.Message, ex);
            //}
        }

        protected virtual AlgoliaIndexDocument ConvertToProviderDocument(IndexDocument document, string documentType)
        {
            var result = new AlgoliaIndexDocument { ObjectID = document.Id };

            result.Add(AlgoliaSearchHelper.RawKeyFieldName, document.Id);

            foreach (var field in document.Fields.OrderBy(f => f.Name))
            {
                var fieldName = field.Name;

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
                    // TODO: handle GEO POINT
                    var point = field.Value as GeoPoint;
                    var value = field.Value;

                    result.Add(fieldName, value);
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
            //throw new SearchException($"{message}. Search service name: {_azureSearchOptions.SearchServiceName}, Scope: {_searchOptions.Scope}", innerException);
        }


        protected virtual SearchClient CreateSearchServiceClient()
        {
            var result = new SearchClient(_azureSearchOptions.AppId, _azureSearchOptions.ApiKey);
            return result;
        }
    }
}
