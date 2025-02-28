using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using VirtoCommerce.AlgoliaSearchModule.Core;
using VirtoCommerce.AlgoliaSearchModule.Data;
using VirtoCommerce.Platform.Core.Modularity;
using VirtoCommerce.Platform.Core.Security;
using VirtoCommerce.Platform.Core.Settings;
using VirtoCommerce.SearchModule.Core.Extensions;

namespace VirtoCommerce.AlgoliaSearchModule.Web
{
    public class Module : IModule, IHasConfiguration
    {
        public ManifestModuleInfo ModuleInfo { get; set; }
        public IConfiguration Configuration { get; set; }

        public void Initialize(IServiceCollection serviceCollection)
        {
            if (Configuration.SearchProviderActive(ModuleConstants.ProviderName))
            {
                serviceCollection.Configure<AlgoliaSearchOptions>(Configuration.GetSection($"Search:{ModuleConstants.ProviderName}"));

                serviceCollection.AddSingleton<IAlgoliaSearchRequestBuilder, AlgoliaSearchRequestBuilder>();
                serviceCollection.AddSingleton<IAlgoliaSearchResponseBuilder, AlgoliaSearchResponseBuilder>();
                serviceCollection.AddSingleton<AlgoliaSearchProvider>();
            }
        }

        public void PostInitialize(IApplicationBuilder appBuilder)
        {
            var settingsRegistrar = appBuilder.ApplicationServices.GetRequiredService<ISettingsRegistrar>();
            settingsRegistrar.RegisterSettings(ModuleConstants.Settings.AllSettings, ModuleInfo.Id);

            var permissionsRegistrar = appBuilder.ApplicationServices.GetRequiredService<IPermissionsRegistrar>();
            permissionsRegistrar.RegisterPermissions(ModuleInfo.Id, ModuleConstants.ModuleName, ModuleConstants.Security.Permissions.AllPermissions);

            if (Configuration.SearchProviderActive(ModuleConstants.ProviderName))
            {
                appBuilder.UseSearchProvider<AlgoliaSearchProvider>(ModuleConstants.ProviderName);
            }
        }

        public void Uninstall()
        {
            // not needed
        }
    }
}
