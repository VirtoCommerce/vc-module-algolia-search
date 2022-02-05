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
            if (sortings != null && sortings.Count > 0)
            {
                var sorting = sortings[0];

                var fieldName = ToAlgoliaFieldName(sorting.FieldName);

                // ignore special field called score (it is default elastic search field that we don't need to use, and use default index instead)
                if (fieldName.Equals("score", System.StringComparison.OrdinalIgnoreCase))
                    return masterIndexName;

                // ignore priority sort, use default index for that instead
                if (fieldName.Equals("priority", System.StringComparison.OrdinalIgnoreCase))
                    return masterIndexName;

                // we will support only one field for sorting
                //foreach (var sorting in sortings)
                //{
                //if(!string.IsNullOrEmpty(name))
                //{
                //    name = $"{name}_";
                //}

                if (sorting.IsDescending)
                        name = $"{name}{fieldName}_desc";
                    else
                        name = $"{name}{fieldName}_asc";
                //}
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
