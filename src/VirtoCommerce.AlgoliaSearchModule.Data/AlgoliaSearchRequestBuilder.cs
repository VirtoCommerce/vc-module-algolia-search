using System;
using System.Collections.Generic;
using System.Linq;
using Algolia.Search.Models.Search;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.SearchModule.Core.Model;
using SearchRequest = VirtoCommerce.SearchModule.Core.Model.SearchRequest;

namespace VirtoCommerce.AlgoliaSearchModule.Data
{
    public class AlgoliaSearchRequestBuilder
    {
        // Used to map 'score' sort field to Elastic Search _score sorting field 
        private const string Score = "score";

        public Query BuildRequest(SearchRequest request, string indexName)
        {
            var query = new Query(request.SearchKeywords)
            {
                Offset = request?.Skip,
                Length = request?.Take,
                RestrictSearchableAttributes = request?.SearchFields?.ToList().Select(x=>x.ToLowerInvariant()).ToList(),
                Filters = GetFilters(request),
                Facets = GetAggregations(request),
                //FacetFilters = GetFilters(request),
                //NumericFilters = GetNumericFilters(request)
            };

            /*

            var result = new Nest.SearchRequest(indexName)
            {
                Query = GetQuery(request),
                PostFilter = GetFilters(request, availableFields),
                Aggregations = GetAggregations(request, availableFields),
                Sort = GetSorting(request?.Sorting),
                From = request?.Skip,
                Size = request?.Take,
                TrackScores = request?.Sorting?.Any(x => x.FieldName.EqualsInvariant(Score)) ?? false
            };

            if (request?.IncludeFields != null && request.IncludeFields.Any())
            {
                result.Source = GetSourceFilters(request);
            }

            if (request?.Take == 1)
            {
                result.TrackTotalHits = true;
            }
            */

            return query;
        }

        private string GetFilters(SearchRequest request)
        {
            return GetFilterQueryRecursive(request.Filter);
        }

        //protected virtual SourceFilter GetSourceFilters(SearchRequest request)
        //{
        //    SourceFilter result = null;
        //    if (request?.IncludeFields != null)
        //    {
        //        return new SourceFilter { Includes = request.IncludeFields.ToArray() };
        //    }

        //    return result;
        //}

        //protected virtual Query GetQuery(SearchRequest request)
        //{
        //    Query result = null;

        //    if (!string.IsNullOrEmpty(request?.SearchKeywords))
        //    {
        //        var keywords = request.SearchKeywords;
        //        var fields = request.SearchFields?.Select(AlgoliaSearchHelper.ToElasticFieldName).ToArray() ?? new[] { "_all" };

        //        var multiMatch = new Query(keywords);

        //        if (request.IsFuzzySearch)
        //        {
        //            multiMatch.Fuzziness = request.Fuzziness != null ? Fuzziness.EditDistance(request.Fuzziness.Value) : Fuzziness.Auto;
        //        }

        //        result = multiMatch;
        //    }

        //    return result;
        //}

        //protected virtual IList<ISort> GetSorting(IEnumerable<SortingField> fields)
        //{
        //    var result = fields?.Select(GetSortingField).ToArray();
        //    return result;
        //}

        //protected virtual ISort GetSortingField(SortingField field)
        //{
        //    ISort result;

        //    if (field is GeoDistanceSortingField geoSorting)
        //    {
        //        result = new GeoDistanceSort
        //        {
        //            Field = AlgoliaSearchHelper.ToElasticFieldName(field.FieldName),
        //            Points = new[] { geoSorting.Location.ToGeoLocation() },
        //            Order = geoSorting.IsDescending ? SortOrder.Descending : SortOrder.Ascending,
        //        };
        //    }
        //    else if (field.FieldName.EqualsInvariant(Score))
        //    {
        //        result = new FieldSort
        //        {
        //            Field = new Field("_score"),
        //            Order = field.IsDescending ? SortOrder.Descending : SortOrder.Ascending
        //        };
        //    }
        //    else
        //    {
        //        result = new FieldSort
        //        {
        //            Field = AlgoliaSearchHelper.ToElasticFieldName(field.FieldName),
        //            Order = field.IsDescending ? SortOrder.Descending : SortOrder.Ascending,
        //            Missing = "_last",
        //            UnmappedType = FieldType.Long,
        //        };
        //    }

        //    return result;
        //}

        //protected virtual QueryContainer GetFilters(SearchRequest request, Properties<IProperties> availableFields)
        //{
        //    return GetFilterQueryRecursive(request?.Filter, availableFields);
        //}

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
                if(!result.IsNullOrEmpty())
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
                if(value.IncludeLower)
                {
                    lowerFilter = $"{fieldName}>={value.Lower}";
                }
                else
                {
                    lowerFilter = $"{fieldName}>{value.Lower}";
                }
            }

            var upperFilter = string.Empty;
            if (!string.IsNullOrEmpty(value.Upper))
            {
                if (value.IncludeUpper)
                {
                    upperFilter = $"{fieldName}<={value.Upper}";
                }
                else
                {
                    upperFilter = $"{fieldName}<{value.Upper}";
                }
            }

            if(!string.IsNullOrEmpty(lowerFilter) && !string.IsNullOrEmpty(upperFilter))
            {
                return $"{lowerFilter} AND {upperFilter}";
            }

            return $"{lowerFilter}{upperFilter}";
        }

        /// <summary>
        /// Algolia doesn't support dynamically filtered facets, so return just a list of facet names
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        protected virtual List<string> GetAggregations(SearchRequest request)
        {
            if (request?.Aggregations != null)
            {
                return request.Aggregations.Select(x => AlgoliaSearchHelper.ToAlgoliaFieldName(x.FieldName)).ToList();
            }

            return null;
        }
    }
}
