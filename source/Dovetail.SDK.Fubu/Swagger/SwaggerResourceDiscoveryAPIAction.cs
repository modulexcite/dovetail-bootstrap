using System;
using System.Collections.Generic;
using System.Linq;
using FubuMVC.Core;
using FubuMVC.Core.Http;
using FubuMVC.Core.Urls;

namespace Dovetail.SDK.Fubu.Swagger
{
    public class SwaggerResourceDiscoveryAPIRequest
    {
        [RouteInput]
        public string GroupKey { get; set; }
    }

    public class SwaggerResourceDiscoveryAPIAction
    {
        private readonly ApiFinder _apiActionFinder;
        private readonly ISwaggerMapper _swaggerMapper;
        private readonly IUrlRegistry _urlRegistry;
        private readonly ICurrentHttpRequest _currentHttpRequest;

        public SwaggerResourceDiscoveryAPIAction(ApiFinder apiActionFinder, ISwaggerMapper swaggerMapper, IUrlRegistry urlRegistry, ICurrentHttpRequest currentHttpRequest)
        {
            _apiActionFinder = apiActionFinder;
            _swaggerMapper = swaggerMapper;
            _urlRegistry = urlRegistry;
            _currentHttpRequest = currentHttpRequest;
        }

        //[AsymmetricJson]
        public Resource Execute(SwaggerResourceDiscoveryAPIRequest request)
        {
            var baseUrl = _urlRegistry.UrlFor(request);
            var absoluteBaseUrl = _currentHttpRequest.ToFullUrl(baseUrl);

            var groupActions = _apiActionFinder.ActionsForGroup(request.GroupKey).ToArray();

            var apis = groupActions
                .Select(a =>
                            {
                                //UGH we need to make relative URLs for swagger to be happy. 
                                var pattern = a.ParentChain().Route.Pattern;
                                var resourceUrl = baseUrl.UrlRelativeTo(pattern);

                                //TODO make this detail come from attribute?
                                var description = "Something pithy about this resource api.";

                                return new API
                                           {
                                               path = resourceUrl,
                                               description = description,
                                               operations = _swaggerMapper.OperationsFrom(a).ToArray()
                                           };
                            }).ToArray();

            var typeSet = new HashSet<Type>();
            groupActions.Each(a =>
                            {
                                if(a.HasInput) typeSet.Add(a.InputType());
                                if (a.HasOutput) typeSet.Add(a.OutputType());
                            });

            return new Resource
                       {
                           basePath = absoluteBaseUrl,
                           resourcePath = "/" + request.GroupKey, //HACK
                           apiVersion = "0.2",
                           swaggerVersion = "1.0",
                           apis = apis,
                           models = typeSet.ToArray()
                       };
        }
    }
}