using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Algolia.Search.Models.Search;
using VirtoCommerce.SearchModule.Core.Model;
using SearchRequest = VirtoCommerce.SearchModule.Core.Model.SearchRequest;

namespace VirtoCommerce.AlgoliaSearchModule.Data
{
    public static class AlgoliaSearchResponseBuilder
    {
        public static SearchResponse ToSearchResponse(this SearchResponse<SearchDocument> response, SearchRequest request)
        {
            return new SearchResponse
            {
                TotalCount = (long)response.NbHits,
                Documents = response.Hits.Select(ToSearchDocument).ToList(),
                Aggregations = GetAggregations(response.Facets, request)
            };
        }

        public static SearchDocument ToSearchDocument(SearchDocument hit)
        {
            var result = new SearchDocument { Id = hit[AlgoliaSearchHelper.RawKeyFieldName].ToString() };

            if (hit is IDictionary<string, object> fields)
            {
                foreach (var kvp in fields)
                {
                    var name = kvp.Key;
                    if (kvp.Value is JsonElement jsonElement)
                    {
                        result.Add(name, ConvertJsonElement(jsonElement));
                    }
                }
            }

            return result;
        }

        private static object ConvertJsonElement(JsonElement jsonElement)
        {
            return jsonElement.ValueKind switch
            {
                JsonValueKind.Array => jsonElement.EnumerateArray().Select(x => x.ToString()).ToArray(),
                JsonValueKind.String => jsonElement.GetString(),
                JsonValueKind.Number => jsonElement.GetDouble(),
                JsonValueKind.True or JsonValueKind.False => jsonElement.GetBoolean(),
                JsonValueKind.Object => jsonElement.ToString(),
                JsonValueKind.Null => null,
                _ => throw new InvalidOperationException($"Unsupported JsonValueKind: {jsonElement.ValueKind}")
            };
        }

        private static IList<AggregationResponse> GetAggregations(Dictionary<string, Dictionary<string, int>> searchResponseAggregations, SearchRequest request)
        {
            var result = new List<AggregationResponse>();

            if (request?.Aggregations != null && searchResponseAggregations != null)
            {
                foreach (var field in searchResponseAggregations.Keys)
                {
                    if (searchResponseAggregations.Values.Any())
                    {
                        var requestAggregation = request.Aggregations.SingleOrDefault(
                            x => (!string.IsNullOrEmpty(x.FieldName) && AlgoliaSearchHelper.ToAlgoliaFieldName(x.FieldName).Equals(field, StringComparison.OrdinalIgnoreCase))
                                 || (!string.IsNullOrEmpty(x.Id) && AlgoliaSearchHelper.ToAlgoliaFieldName(x.Id).Equals(field, StringComparison.OrdinalIgnoreCase)));

                        if (requestAggregation != null)
                        {
                            IList<string> requestValues = (requestAggregation as TermAggregationRequest)?.Values;

                            var aggregation = new AggregationResponse
                            {
                                Id = string.IsNullOrEmpty(requestAggregation.FieldName) ? requestAggregation.Id : requestAggregation.FieldName,
                                Values = searchResponseAggregations[field]
                                    .Where(x => requestValues == null || requestValues.Contains(x.Key))
                                    .Select(x => new AggregationResponseValue { Id = x.Key, Count = x.Value })
                                    .ToList()
                            };

                            result.Add(aggregation);
                        }
                    }
                }
            }

            return result;
        }
    }
}
