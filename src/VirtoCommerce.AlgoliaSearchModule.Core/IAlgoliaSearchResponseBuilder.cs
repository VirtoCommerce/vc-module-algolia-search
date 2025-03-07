using Algolia.Search.Models.Search;
using VirtoCommerce.SearchModule.Core.Model;

namespace VirtoCommerce.AlgoliaSearchModule.Core;

public interface IAlgoliaSearchResponseBuilder
{
    SearchResponse ToSearchResponse(SearchResponses<SearchDocument> response, SearchRequest request);
}
