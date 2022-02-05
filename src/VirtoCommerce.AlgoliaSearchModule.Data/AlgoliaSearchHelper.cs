using System.Collections.Generic;
using VirtoCommerce.SearchModule.Core.Model;

namespace VirtoCommerce.AlgoliaSearchModule.Data
{
    public static class AlgoliaSearchHelper
    {
        public const string RawKeyFieldName = "objectID";

        public static string ToAlgoliaFieldName(string originalName)
        {
            return originalName?.ToLowerInvariant().Trim().Replace(' ', '_');
        }

        public static string ToAlgoliaIndexName(string masterIndexName, IList<SortingField> sortings)
        {
            var name = string.Empty;

            // use master index if no sorting defined
            if (sortings != null)
            {
                foreach (var sorting in sortings)
                {
                    if(!string.IsNullOrEmpty(name))
                    {
                        name = $"{name}_";
                    }

                    if (sorting.IsDescending)
                        name = $"{name}{ToAlgoliaFieldName(sorting.FieldName)}_desc";
                    else
                        name = $"{name}{ToAlgoliaFieldName(sorting.FieldName)}_asc";
                }
            }

            return $"{masterIndexName}_{name}";
        }

        public static string ToAlgoliaReplicaName(string masterIndexName, AlgoliaIndexSortReplica replica)
        {
            var name = string.Empty;
            if(replica.IsDescending)
                name = $"{ToAlgoliaFieldName(replica.FieldName)}_desc";
            else
                name = $"{ToAlgoliaFieldName(replica.FieldName)}_asc";

            if(replica.IsVirtual)
            {
                name = $"virtual({name})";
            }

            return $"{masterIndexName}_{name}";
        }

        /*
        public static GeoLocation ToGeoLocation(this GeoPoint point)
        {
            return point == null ? null : new GeoLocation(point.Latitude, point.Longitude);
        }
        */
    }
}
