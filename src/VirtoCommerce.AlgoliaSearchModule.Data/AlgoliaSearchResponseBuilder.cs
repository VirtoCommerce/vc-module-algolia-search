using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Algolia.Search.Models.Search;
using VirtoCommerce.AlgoliaSearchModule.Data.Extensions;
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

        public static SearchDocument ToSearchDocument(SearchDocument fields)
        {
            var result = new SearchDocument { Id = fields[AlgoliaSearchHelper.RawKeyFieldName].ToString() };

            foreach (var kvp in fields)
            {
                var name = kvp.Key;
                if (kvp.Value is JsonElement jsonElement)
                {
                    if (IsDateTimeField(name))
                    {
                        result.Add(name, DateTimeExtension.UnixTimestampToDateTime((long)(double)ConvertJsonElement(jsonElement)));
                    }
                    else
                    {
                        result.Add(name, ConvertJsonElement(jsonElement));
                    }
                }
                else
                {
                    result.Add(name, kvp.Value);
                }
            }

            return result;
        }

        private static bool IsDateTimeField(string name)
        {
            return name.Equals("indexationdate", StringComparison.OrdinalIgnoreCase) ||
                name.Equals("createddate", StringComparison.OrdinalIgnoreCase) ||
                name.Equals("modifieddate", StringComparison.OrdinalIgnoreCase);
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
                    if (searchResponseAggregations.Values.Count > 0)
                    {
                        var requestAggregation = request.Aggregations.SingleOrDefault(
                            x => (!string.IsNullOrEmpty(x.FieldName) && AlgoliaSearchHelper.ToAlgoliaFieldName(x.FieldName).Equals(field, StringComparison.OrdinalIgnoreCase))
                                 || (!string.IsNullOrEmpty(x.Id) && AlgoliaSearchHelper.ToAlgoliaFieldName(x.Id).Equals(field, StringComparison.OrdinalIgnoreCase)));

                        if (requestAggregation != null)
                        {
                            var requestValues = (requestAggregation as TermAggregationRequest)?.Values;

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
