using System;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using VirtoCommerce.AlgoliaSearchModule.Data;
using VirtoCommerce.SearchModule.Core.Model;
using VirtoCommerce.SearchModule.Core.Services;
using Xunit;

namespace VirtoCommerce.AlgoliaSearchModule.Tests
{
    [Trait("Category", "CI")]
    [Trait("Category", "IntegrationTest")]
    public class AlgoliaSearchTests : SearchProviderTests, IDisposable
    {

        public AlgoliaSearchTests()
        {
            var provider = GetSearchProvider();

            // Delete index
            // provider.DeleteIndexAsync(DocumentType).Wait();

            // Create index and add documents
            var primaryDocuments = GetPrimaryDocuments();

            var response = provider.IndexAsync(DocumentType, primaryDocuments).Result;
            var secondaryDocuments = GetSecondaryDocuments();
            response = provider.IndexAsync(DocumentType, secondaryDocuments).Result;
        }

        protected override ISearchProvider GetSearchProvider()
        {
            var appId = Environment.GetEnvironmentVariable("AlgoliaAppId");
            var apiLKey = Environment.GetEnvironmentVariable("AlgoliaApiKey");

            var elasticOptions = Options.Create(
                new AlgoliaSearchOptions
                {
                    AppId = appId,
                    ApiKey = apiLKey
                }
            );
            var searchOptions = Options.Create(new SearchOptions { Scope = "test-core", Provider = "AlgoliaSearch" });

            var loggerMock = new Mock<ILogger<AlgoliaSearchProvider>>();
            var provider = new AlgoliaSearchProvider(elasticOptions, searchOptions, GetSettingsManager(), loggerMock.Object);
            return provider;
        }

        protected virtual void Dispose(bool disposing)
        {
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
