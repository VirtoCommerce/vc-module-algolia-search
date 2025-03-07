using Algolia.Search.Models.Search;
using VirtoCommerce.SearchModule.Core.Model;

namespace VirtoCommerce.AlgoliaSearchModule.Core;

public interface IAlgoliaSearchRequestBuilder
{
    SearchForHits BuildSearchForHits(string indexName, SearchRequest request);

    SearchForFacets BuildSearchForFacets(string indexName, SearchRequest request, AggregationRequest aggregation);
}
