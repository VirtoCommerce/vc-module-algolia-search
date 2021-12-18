using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using VirtoCommerce.AlgoliaSearchModule.Data;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.Modularity;
using VirtoCommerce.Platform.Core.Settings;
using VirtoCommerce.SearchModule.Core.Services;

namespace VirtoCommerce.AlgoliaSearchModule.Web
{
    public class Module : IModule, IHasConfiguration
    {
        public ManifestModuleInfo ModuleInfo { get; set; }
        public IConfiguration Configuration { get; set; }

        public void Initialize(IServiceCollection serviceCollection)
        {
            var provider = Configuration.GetValue<string>("Search:Provider");

            if (provider.EqualsInvariant("AlgoliaSearch"))
            {
                serviceCollection.Configure<AlgoliaSearchOptions>(Configuration.GetSection("Search:AlgoliaSearch"));
                serviceCollection.AddSingleton<ISearchProvider, AlgoliaSearchProvider>();
            }
        }

        public void PostInitialize(IApplicationBuilder appBuilder)
        {
            var settingsRegistrar = appBuilder.ApplicationServices.GetRequiredService<ISettingsRegistrar>();
            settingsRegistrar.RegisterSettings(ModuleConstants.Settings.AllSettings, ModuleInfo.Id);
        }

        public void Uninstall()
        {
            // not needed
        }
    }
}
