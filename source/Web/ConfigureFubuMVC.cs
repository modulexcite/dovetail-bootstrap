using System;
using Bootstrap.Web.Handlers;
using Bootstrap.Web.Handlers.error.http500;
using Bootstrap.Web.Handlers.home;
using Bootstrap.Web.Security;
using Dovetail.SDK.Fubu.Clarify.Lists;
using Dovetail.SDK.Fubu.TokenAuthentication.Token;
using FubuCore;
using FubuMVC.Core;
using FubuMVC.Core.Registration.Nodes;
using FubuMVC.Core.Security;
using FubuMVC.Swagger;
using FubuMVC.Swagger.Configuration;

namespace Bootstrap.Web
{
    public class ConfigureFubuMVC : FubuRegistry
    {
        public ConfigureFubuMVC()
        {
            Import<HandlerConvention>(x => x.MarkerType<HandlerMarker>());
            
			Routes.HomeIs<HomeRequest>();

            Policies.Add<AuthenticationTokenConvention>();
            
            //convention to transfer exceptions to the view for an input model given via generic argument
			Policies.Add<APIExceptionConvention<Error500Request>>();

			Import<BootstrapHtmlConvention>();

            //TODO replace this with Swagger Bottle
            Policies.Add<SwaggerConvention>();

            Services(s=>
                         {
                             s.ReplaceService<IAuthorizationFailureHandler, BootstrapAuthorizationFailureHandler>();

                             s.AddService<IActionGrouper, APIRouteGrouper>();
							 s.AddService<IActionFinder, BootstrapActionFinder>();
                         });
        }
    }

	public class BootstrapActionFinder : IActionFinder
	{
		public Func<ActionCall, bool> Matches { get { return IsApiCall;} }

		private static bool IsApiCall(ActionCall actionCall)
		{
			return actionCall.ParentChain().InputType().CanBeCastTo<Dovetail.SDK.Bootstrap.IApi>();
		}
	}
}