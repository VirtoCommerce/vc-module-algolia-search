using System;
using Microsoft.Extensions.Options;
using VirtoCommerce.AlgoliaSearchModule.Data;
using VirtoCommerce.SearchModule.Core.Model;
using VirtoCommerce.SearchModule.Core.Services;
using Xunit;

namespace VirtoCommerce.AlgoliaSearchModule.Tests
{
    [Trait("Category", "CI")]
    [Trait("Category", "IntegrationTest")]
    public class AlgoliaSearchTests : SearchProviderTests
    {
        protected override ISearchProvider GetSearchProvider()
        {
            var appId = Environment.GetEnvironmentVariable("AlgoliaAppId");
            var apiLKey = Environment.GetEnvironmentVariable("AlgoliaApiKey");

            var elasticOptions = Options.Create(new AlgoliaSearchOptions { AppId = appId,  ApiKey = apiLKey });
            var searchOptions = Options.Create(new SearchOptions { Scope = "test-core", Provider = "AlgoliaSearch" });

            var provider = new AlgoliaSearchProvider(elasticOptions, searchOptions, GetSettingsManager());
            return provider;
        }
    }
}
