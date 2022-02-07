using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using VirtoCommerce.Platform.Core.Settings;

namespace VirtoCommerce.AlgoliaSearchModule.Web
{
    [ExcludeFromCodeCoverage]
    public static class ModuleConstants
    {
        public static class Settings
        {
            public static class Indexing
            {
                public static readonly SettingDescriptor SortReplicas = new()
                {
                    Name = "VirtoCommerce.Search.AlgoliaSearch.SortReplicas",
                    GroupName = "Search|AlgoliaSearch",
                    IsDictionary = true,
                    ValueType = SettingValueType.ShortText,
                    AllowedValues = new[] { "product:name-asc", "product:name-desc", "product:price-asc", "product:price-desc", "indexationdate_timestamp-desc" },
                    DefaultValue = new[] { "product:name-asc", "product:name-desc", "product:price-asc", "product:price-desc", "indexationdate_timestamp-desc" }
                };

                private static readonly SettingDescriptor VirtualSortReplicas = new()
                {
                    Name = "VirtoCommerce.Search.AlgoliaSearch.Version",
                    GroupName = "Search|AlgoliaSearch",
                    ValueType = SettingValueType.ShortText,
                    AllowedValues = new[] { "Standard", "Premium" },
                    DefaultValue = "Standard"
                };

                public static IEnumerable<SettingDescriptor> AllSettings
                {
                    get
                    {
                        yield return VirtualSortReplicas;
                        yield return SortReplicas;              
                    }
                }
            }

            public static IEnumerable<SettingDescriptor> AllSettings => Indexing.AllSettings;
        }
    }
}
