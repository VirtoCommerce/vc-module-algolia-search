using System;
using System.Collections.Generic;
using System.Linq;
using Algolia.Search.Models.Search;
using Newtonsoft.Json.Linq;
using VirtoCommerce.SearchModule.Core.Model;
using SearchRequest = VirtoCommerce.SearchModule.Core.Model.SearchRequest;

namespace VirtoCommerce.AlgoliaSearchModule.Data
{
    public static class AlgoliaSearchResponseBuilder
    {
        public static SearchResponse ToSearchResponse(this SearchResponse<SearchDocument> response, SearchRequest request)
        {
            var result = new SearchResponse
            {
                TotalCount = response.NbHits,
                Documents = response.Hits.Select(ToSearchDocument).ToList(),
                Aggregations = GetAggregations(response.Facets, request)
            };

            return result;
        }

        public static SearchDocument ToSearchDocument(SearchDocument hit)
        {
            var result = new SearchDocument { Id = hit[AlgoliaSearchHelper.RawKeyFieldName].ToString() };

            // Copy fields and convert JArray to object[]
            var fields = (IDictionary<string, object>)hit;

            if (fields != null)
            {
                foreach (var kvp in fields)
                {
                    var name = kvp.Key;
                    var value = kvp.Value;

                    if (value is JArray jArray)
                    {
                        value = jArray.ToObject<object[]>();
                    }

                    result.Add(name, value);
                }
            }

            return result;
        }

        private static IList<AggregationResponse> GetAggregations(Dictionary<string, Dictionary<string, long>> searchResponseAggregations, SearchRequest request)
        {
            var result = new List<AggregationResponse>();

            if (request?.Aggregations != null && searchResponseAggregations != null)
            {
                foreach (var field in searchResponseAggregations.Keys)
                {
                    if (searchResponseAggregations.Values.Any())
                    {
                        IList<string> requestValues = null;
                        var requestAggregation = request.Aggregations.Where(
                                x =>
                                    (!string.IsNullOrEmpty(x.FieldName) && AlgoliaSearchHelper.ToAlgoliaFieldName(x.FieldName).Equals(field, StringComparison.OrdinalIgnoreCase))
                                    ||
                                    (!string.IsNullOrEmpty(x.Id) && AlgoliaSearchHelper.ToAlgoliaFieldName(x.Id).Equals(field, StringComparison.OrdinalIgnoreCase))
                            ).SingleOrDefault();
                        if(requestAggregation is TermAggregationRequest)
                        {
                            requestValues = ((TermAggregationRequest)requestAggregation).Values;
                        }

                        var aggregation = new AggregationResponse
                        {
                            Id = string.IsNullOrEmpty(requestAggregation.FieldName) ? requestAggregation.Id : requestAggregation.FieldName,
                            Values = searchResponseAggregations[field].
                            Where(
                                x=>requestValues == null || requestValues.Contains(x.Key))
                            .Select(x => new AggregationResponseValue() { Id = x.Key, Count = x.Value }).ToList()
                        };

                        result.Add(aggregation);
                    }
                }
            }

            return result;
        }
    }
}
