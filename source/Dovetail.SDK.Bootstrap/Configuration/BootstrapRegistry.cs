using Dovetail.SDK.Bootstrap.Authentication;
using Dovetail.SDK.Bootstrap.Clarify;
using Dovetail.SDK.Bootstrap.History.AssemblerPolicies;
using Dovetail.SDK.Bootstrap.History.Configuration;
using Dovetail.SDK.Bootstrap.History.TemplatePolicies;
using FChoice.Foundation.Clarify;
using FChoice.Foundation.Schema;
using FubuLocalization;
using StructureMap.Configuration.DSL;
using StructureMap.Graph;
using ILocaleCache = FChoice.Foundation.Clarify.ILocaleCache;

namespace Dovetail.SDK.Bootstrap.Configuration
{
	public class BootstrapRegistry : Registry
	{
		public static bool MaintainAspNetCompatibility = true;

		public BootstrapRegistry()
		{
			For<ISecurityContext>().Use(c => c.GetInstance<SecurityContextProvider>().GetSecurityContext());

			For<ILogger>()
				.AlwaysUnique()
				.Use(s => s.ParentType == null ? new Log4NetLogger(s.RootType) : new Log4NetLogger(s.ParentType));

			Scan(s =>
			{
				s.TheCallingAssembly();
				s.AssemblyContainingType<IClarifySessionCache>();
				s.WithDefaultConventions();
				s.AddAllTypesOf<IHistoryAssemblerPolicy>();
			});

			//IncludeRegistry<AppSettingProviderRegistry>();

			ForSingletonOf<IClarifyApplicationFactory>().Use<ClarifyApplicationFactory>();
			ForSingletonOf<IClarifyApplication>().Use(ctx => ctx.GetInstance<IClarifyApplicationFactory>().Create());

			//configure the container to use the session cache as a factory for the current user's session
			//any web class that takes a dependency on IClarifySession will get a session for the current
			//authenticated user.
			ForSingletonOf<IClarifySessionCache>().Use<ClarifySessionCache>();
			For<IClarifySession>().TransientOrHybridHttpScoped().Use(ctx => ctx.GetInstance<IClarifySessionProvider>().CreateSession());

			For<IApplicationClarifySessionFactory>().Use<DefaultApplicationClarifySessionFactory>();
			For<IApplicationClarifySession>().Use(ctx => ctx.GetInstance<IApplicationClarifySessionFactory>().Create());

			//Make Dovetail SDK caches directly available for DI.
			For<ISchemaCache>().Use(c => c.GetInstance<IClarifyApplication>().SchemaCache);
			For<IStringCache>().Use(c => c.GetInstance<IClarifyApplication>().StringCache);
			For<ILocaleCache>().Use(c => c.GetInstance<IClarifyApplication>().LocaleCache);
			For<IListCache>().Use(c => c.GetInstance<IClarifyApplication>().ListCache);

			ForSingletonOf<IRequestPathAuthenticationPolicy>().Use<RequestPathAuthenticationPolicy>();

			For<IOutputEncoder>().Use<HtmlEncodeOutputEncoder>();

			//It is the responsibility of the applicationUrl using bootstrap to set the current sdk user's login
			For<ICurrentSDKUser>().TransientOrHybridHttpScoped().Use<CurrentSDKUser>();
			For<IUserClarifySessionConfigurator>().Use<UTCTimezoneUserClarifySessionConfigurator>();
			For<IDatabaseTime>().TransientOrHybridHttpScoped().Use<DatabaseTime>();

			For<ILocalizationMissingHandler>().Use<BootstrapLocalizationMissingHandler>();

			For<IWebApplicationUrl>().Use<AspNetWebApplicationUrl>();
			For<IHistoryOriginalMessageConfiguration>().Singleton().Use<HistoryOriginalMessageConfiguration>();

			this.ActEntryTemplatePolicies<DefaultActEntryTemplatePolicyRegistry>();
		}
	}
}
