using System;
using System.Collections.Generic;
using System.Linq;
using Algolia.Search.Models.Search;
using VirtoCommerce.AlgoliaSearchModule.Data.Extensions;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.SearchModule.Core.Model;
using SearchRequest = VirtoCommerce.SearchModule.Core.Model.SearchRequest;

namespace VirtoCommerce.AlgoliaSearchModule.Data
{
    public class AlgoliaSearchRequestBuilder
    {
        public SearchParamsObject BuildRequest(SearchRequest request, string indexName)
        {
            var query = new SearchParamsObject
            {
                Query = request.SearchKeywords,
                Offset = request?.Skip,
                Length = request?.Take,
                RestrictSearchableAttributes = GetSearchableAttributes(request),
                Filters = GetFilters(request),
                Facets = GetAggregations(request),
                AroundLatLng = GetGeoFilter(request)
            };

            return query;
        }

        protected List<string> GetSearchableAttributes(SearchRequest request)
        {
            // Ignore default _content field
            return request?.SearchFields?.ToList()
                .Where(x => !x.ToLowerInvariant().Equals("_content"))
                .Select(x => x.ToLowerInvariant()).ToList();
        }

        protected string GetFilters(SearchRequest request)
        {
            return GetFilterQueryRecursive(request.Filter);
        }

        protected string GetGeoFilter(SearchRequest request)
        {
            if (request.Sorting != null && request.Sorting.Count > 0 && request.Sorting[0] is GeoDistanceSortingField)
            {
                var sort = request.Sorting[0] as GeoDistanceSortingField;
                return $"{sort.Location.Latitude}, {sort.Location.Longitude}";
            }

            return null;
        }

        protected virtual string GetFilterQueryRecursive(IFilter filter)
        {
            var result = string.Empty;

            switch (filter)
            {
                case IdsFilter idsFilter:
                    result = CreateIdsFilter(idsFilter);
                    break;

                case TermFilter termFilter:
                    result = CreateTermFilter(termFilter);
                    break;

                case RangeFilter rangeFilter:
                    result = CreateRangeFilter(rangeFilter);
                    break;

                //case GeoDistanceFilter geoDistanceFilter:
                //    result = CreateGeoDistanceFilter(geoDistanceFilter);
                //    break;

                case NotFilter notFilter:
                    result = CreateNotFilter(notFilter);
                    break;

                case AndFilter andFilter:
                    result = CreateAndFilter(andFilter);
                    break;

                case OrFilter orFilter:
                    result = CreateOrFilter(orFilter);
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

        protected virtual string CreateTermFilter(TermFilter termFilter)
        {
            var result = string.Empty;

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

        protected virtual string CreateRangeFilter(RangeFilter rangeFilter)
        {
            var result = string.Empty;

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

        protected virtual string CreateNotFilter(NotFilter notFilter)
        {
            var result = string.Empty;

            if (notFilter?.ChildFilter != null)
            {
                result = $"NOT {GetFilterQueryRecursive(notFilter.ChildFilter)}";
            }

            return result;
        }

        protected virtual string CreateAndFilter(AndFilter andFilter)
        {
            string result = string.Empty;

            if (andFilter?.ChildFilters != null)
            {
                foreach (var childQuery in andFilter.ChildFilters)
                {
                    if (!result.IsNullOrEmpty())
                    {
                        result = $"{result} AND ";
                    }
                    result = $"{result}({GetFilterQueryRecursive(childQuery)})";
                }
            }

            return result;
        }

        protected virtual string CreateOrFilter(OrFilter orFilter)
        {
            string result = string.Empty;

            if (orFilter?.ChildFilters != null)
            {
                foreach (var childQuery in orFilter.ChildFilters)
                {
                    if (!result.IsNullOrEmpty())
                    {
                        result = $"{result} OR ";
                    }
                    result = $"{result}({GetFilterQueryRecursive(childQuery)})";
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
