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
                private static readonly SettingDescriptor IndexTotalFieldsLimit = new SettingDescriptor
                {
                    Name = "VirtoCommerce.Search.AlgoliaSearch.IndexTotalFieldsLimit",
                    GroupName = "Search|AlgoliaSearch",
                    ValueType = SettingValueType.Integer,
                    DefaultValue = 1000
                };

                private static readonly SettingDescriptor TokenFilter = new SettingDescriptor
                {
                    Name = "VirtoCommerce.Search.AlgoliaSearch.TokenFilter",
                    GroupName = "Search|AlgoliaSearch",
                    ValueType = SettingValueType.ShortText,
                    DefaultValue = "custom_edge_ngram"
                };

                private static readonly SettingDescriptor MinGram = new SettingDescriptor
                {
                    Name = "VirtoCommerce.Search.AlgoliaSearch.NGramTokenFilter.MinGram",
                    GroupName = "Search|AlgoliaSearch",
                    ValueType = SettingValueType.Integer,
                    DefaultValue = 1
                };

                private static readonly SettingDescriptor MaxGram = new SettingDescriptor
                {
                    Name = "VirtoCommerce.Search.AlgoliaSearch.NGramTokenFilter.MaxGram",
                    GroupName = "Search|AlgoliaSearch",
                    ValueType = SettingValueType.Integer,
                    DefaultValue = 20
                };

                public static IEnumerable<SettingDescriptor> AllSettings
                {
                    get
                    {
                        yield return IndexTotalFieldsLimit;
                        yield return TokenFilter;
                        yield return MinGram;
                        yield return MaxGram;
                    }
                }
            }

            public static IEnumerable<SettingDescriptor> AllSettings => Indexing.AllSettings;
        }
    }
}
