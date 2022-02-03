namespace VirtoCommerce.AlgoliaSearchModule.Data
{
    public static class AlgoliaSearchHelper
    {
        public const string RawKeyFieldName = "objectID";

        public static string ToAlgoliaFieldName(string originalName)
        {
            return originalName?.ToLowerInvariant();
        }

        /*
        public static GeoLocation ToGeoLocation(this GeoPoint point)
        {
            return point == null ? null : new GeoLocation(point.Latitude, point.Longitude);
        }
        */
    }
}
