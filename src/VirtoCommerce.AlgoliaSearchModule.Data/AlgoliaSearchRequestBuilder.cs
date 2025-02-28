using System;
using System.Collections.Generic;
using System.Linq;
using Algolia.Search.Models.Search;
using VirtoCommerce.AlgoliaSearchModule.Core;
using VirtoCommerce.AlgoliaSearchModule.Data.Extensions;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.SearchModule.Core.Model;
using SearchRequest = VirtoCommerce.SearchModule.Core.Model.SearchRequest;

namespace VirtoCommerce.AlgoliaSearchModule.Data
{
    public class AlgoliaSearchRequestBuilder : IAlgoliaSearchRequestBuilder
    {
        public SearchForHits BuildSearchForHits(string indexName, SearchRequest request)
        {
            ArgumentNullException.ThrowIfNullOrEmpty(indexName);
            ArgumentNullException.ThrowIfNull(request);

            var query = new SearchForHits
            {
                IndexName = indexName,
                Query = request.SearchKeywords,
                Offset = request.Skip,
                Length = request.Take,
                RestrictSearchableAttributes = GetSearchableAttributes(request),
                Filters = GetFilters(request),
                Facets = GetAggregations(request),
                AroundLatLng = GetGeoFilter(request)
            };

            return query;
        }

        public SearchForFacets BuildSearchForFacets(string indexName, SearchRequest request, AggregationRequest aggregation)
        {
            ArgumentNullException.ThrowIfNullOrEmpty(indexName);
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(aggregation);

            var query = new SearchForFacets
            {
                IndexName = indexName,
                Query = request.SearchKeywords,
                Offset = 0,
                Length = 0,
                RestrictSearchableAttributes = GetSearchableAttributes(request),
                Filters = GetFilters(request, aggregation.FieldName),
                Facets = [AlgoliaSearchHelper.ToAlgoliaFieldName(aggregation.FieldName)],
                AroundLatLng = GetGeoFilter(request)
            };

            return query;
        }

        protected static List<string> GetSearchableAttributes(SearchRequest request)
        {
            // Ignore default _content field
            return request?.SearchFields?.ToList()
                .Where(x => !x.ToLowerInvariant().Equals("_content"))
                .Select(x => x.ToLowerInvariant()).ToList();
        }

        protected string GetFilters(SearchRequest request, string exlcudedFacetFilter = null)
        {
            return GetFilterQueryRecursive(request.Filter, exlcudedFacetFilter);
        }

        protected static string GetGeoFilter(SearchRequest request)
        {
            if (request.Sorting != null && request.Sorting.Count > 0 && request.Sorting[0] is GeoDistanceSortingField)
            {
                var sort = request.Sorting[0] as GeoDistanceSortingField;
                return $"{sort.Location.Latitude}, {sort.Location.Longitude}";
            }

            return null;
        }

        protected virtual string GetFilterQueryRecursive(IFilter filter, string exlcudeFacetFilter)
        {
            var result = string.Empty;

            switch (filter)
            {
                case IdsFilter idsFilter:
                    result = CreateIdsFilter(idsFilter);
                    break;

                case TermFilter termFilter:
                    result = CreateTermFilter(termFilter, exlcudeFacetFilter);
                    break;

                case RangeFilter rangeFilter:
                    result = CreateRangeFilter(rangeFilter, exlcudeFacetFilter);
                    break;

                //case GeoDistanceFilter geoDistanceFilter:
                //    result = CreateGeoDistanceFilter(geoDistanceFilter);
                //    break;

                case NotFilter notFilter:
                    result = CreateNotFilter(notFilter, exlcudeFacetFilter);
                    break;

                case AndFilter andFilter:
                    result = CreateAndFilter(andFilter, exlcudeFacetFilter);
                    break;

                case OrFilter orFilter:
                    result = CreateOrFilter(orFilter, exlcudeFacetFilter);
                    break;

                    //case WildCardTermFilter wildcardTermFilter:
                    //    result = CreateWildcardTermFilter(wildcardTermFilter);
                    //    break;
            }

            return result;
        }


        protected virtual string CreateIdsFilter(IdsFilter idsFilter)
        {
            string result = string.Empty;

            if (idsFilter?.Values != null)
            {
                foreach (var val in idsFilter.Values)
                {
                    if (!result.IsNullOrEmpty())
                    {
                        result = $"{result} OR ";
                    }
                    result = $"{result}{AlgoliaSearchHelper.RawKeyFieldName}:\"{val}\"";
                }
            }

            return result;
        }

        //protected virtual QueryContainer CreateWildcardTermFilter(WildCardTermFilter wildcardTermFilter)
        //{
        //    return new WildcardQuery
        //    {
        //        Field = AlgoliaSearchHelper.ToElasticFieldName(wildcardTermFilter.FieldName),
        //        Value = wildcardTermFilter.Value
        //    };
        //}

        protected virtual string CreateTermFilter(TermFilter termFilter, string exlcudeFacetFilter)
        {
            var result = string.Empty;

            if (termFilter.FieldName.Equals(exlcudeFacetFilter, StringComparison.OrdinalIgnoreCase))
            {
                return result;
            }

            foreach (var val in termFilter.Values)
            {
                if (!result.IsNullOrEmpty())
                {
                    result = $"{result} OR ";
                }
                result = $"{result}{AlgoliaSearchHelper.ToAlgoliaFieldName(termFilter.FieldName)}:\"{val.ToLowerInvariant()}\"";
            }

            return result;
        }

        protected virtual string CreateRangeFilter(RangeFilter rangeFilter, string exlcudeFacetFilter)
        {
            var result = string.Empty;

            if (rangeFilter.FieldName.Equals(exlcudeFacetFilter, StringComparison.OrdinalIgnoreCase))
            {
                return result;
            }

            var fieldName = AlgoliaSearchHelper.ToAlgoliaFieldName(rangeFilter.FieldName);
            foreach (var value in rangeFilter.Values)
            {
                if (!result.IsNullOrEmpty())
                {
                    result = $"{result} OR ";
                }
                result += CreateTermRangeQuery(fieldName, value);
            }

            return result;
        }

        //protected virtual QueryContainer CreateGeoDistanceFilter(GeoDistanceFilter geoDistanceFilter)
        //{
        //    return new GeoDistanceQuery
        //    {
        //        Field = AlgoliaSearchHelper.ToElasticFieldName(geoDistanceFilter.FieldName),
        //        Location = geoDistanceFilter.Location.ToGeoLocation(),
        //        Distance = new Distance(geoDistanceFilter.Distance, DistanceUnit.Kilometers),
        //    };
        //}

        protected virtual string CreateNotFilter(NotFilter notFilter, string exlcudeFacetFilter)
        {
            var result = string.Empty;

            if (notFilter?.ChildFilter != null)
            {
                var filter = GetFilterQueryRecursive(notFilter.ChildFilter, exlcudeFacetFilter);
                if (!string.IsNullOrEmpty(filter))
                {
                    result = $"NOT {filter}";
                }
            }

            return result;
        }

        protected virtual string CreateAndFilter(AndFilter andFilter, string exlcudeFacetFilter)
        {
            var result = string.Empty;

            if (andFilter?.ChildFilters != null)
            {
                foreach (var childQuery in andFilter.ChildFilters)
                {
                    var filter = GetFilterQueryRecursive(childQuery, exlcudeFacetFilter);
                    if (string.IsNullOrEmpty(filter))
                    {
                        continue;
                    }

                    if (!result.IsNullOrEmpty())
                    {
                        result = $"{result} AND ";
                    }
                    result = $"{result}({filter})";
                }
            }

            return result;
        }

        protected virtual string CreateOrFilter(OrFilter orFilter, string exlcudeFacetFilter)
        {
            var result = string.Empty;

            if (orFilter?.ChildFilters != null)
            {
                foreach (var childQuery in orFilter.ChildFilters)
                {
                    var filter = GetFilterQueryRecursive(childQuery, exlcudeFacetFilter);
                    if (string.IsNullOrEmpty(filter))
                    {
                        continue;
                    }

                    if (!result.IsNullOrEmpty())
                    {
                        result = $"{result} OR ";
                    }
                    result = $"{result}({filter})";
                }
            }

            return result;
        }

        protected virtual string CreateTermRangeQuery(string fieldName, RangeFilterValue value)
        {
            var lowerFilter = string.Empty;
            if (!string.IsNullOrEmpty(value.Lower))
            {
                if (value.IncludeLower)
                {
                    lowerFilter = $"{fieldName}>={GetAlogilaRangeValue(value.Lower)}";
                }
                else
                {
                    lowerFilter = $"{fieldName}>{GetAlogilaRangeValue(value.Lower)}";
                }
            }

            var upperFilter = string.Empty;
            if (!string.IsNullOrEmpty(value.Upper))
            {
                if (value.IncludeUpper)
                {
                    upperFilter = $"{fieldName}<={GetAlogilaRangeValue(value.Upper)}";
                }
                else
                {
                    upperFilter = $"{fieldName}<{GetAlogilaRangeValue(value.Upper)}";
                }
            }

            if (!string.IsNullOrEmpty(lowerFilter) && !string.IsNullOrEmpty(upperFilter))
            {
                return $"{lowerFilter} AND {upperFilter}";
            }

            return $"{lowerFilter}{upperFilter}";
        }

        private static string GetAlogilaRangeValue(string value)
        {
            if (DateTime.TryParse(value, out var dateTime))
            {
                return DateTimeExtension.DateTimeToUnixTimestamp(dateTime).ToString();
            }
            return value;
        }

        /// <summary>
        /// Algolia doesn't support dynamically filtered facets, so return just a list of facet names
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        protected virtual List<string> GetAggregations(SearchRequest request)
        {
            List<string> facets = null;
            if (request?.Aggregations != null)
            {
                facets = request.Aggregations.Select(x => AlgoliaSearchHelper.ToAlgoliaFieldName(string.IsNullOrEmpty(x.FieldName) ? x.Id : x.FieldName)).ToList();
            }

            if (facets != null && facets.Count > 0)
                return facets; // otherwise we should return all facets

            return null;
        }
    }
}
