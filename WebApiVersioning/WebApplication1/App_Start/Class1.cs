using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Controllers;
using System.Web.Http.Dispatcher;

namespace WebApplication1
{
    public class HeaderVersionControllerSelector : DefaultHttpControllerSelector
    {
        // api version key name
        private const string VersionHeaderKey = "api-version";
        private const string GenericUnAuthorized = "Unauthorized access!";
        public const string Controller = "Controller";
        public const string ApiNamespace = "WebApplication1.Controllers.api";
        public const string ApiAppName = "WebApplication1";

        private readonly HttpConfiguration _config;

        public HeaderVersionControllerSelector(HttpConfiguration config) : base(config)
        {
            _config = config;
        }
        public override HttpControllerDescriptor SelectController(HttpRequestMessage request)
        {
            try
            {
                //pull version value from HTTP header
                IEnumerable<string> values;
                int? apiVersion = null;
                if (request.Headers.TryGetValues(VersionHeaderKey, out values))
                {
                    foreach (string value in values)
                    {
                        int version;
                        if (!Int32.TryParse(value, out version)) continue;
                        apiVersion = version;
                        break;
                    }
                }
                //get the name of the route used to identify the controller
                var controllerRouteName = GetControllerNameFromRequest(request);
                // handle special request i.e form submit where we can't add header
                //apiVersion = ApiVersion(request, apiVersion);

                //build up controller name from route and version #
                var controllerName = controllerRouteName + Controller;
                var versionInfo = "";
                if (apiVersion != null)
                {
                    versionInfo = ".V" + apiVersion;
                }
                var controllerNameWithVersion = string.Format("{0}{1}.{2}{3}", ApiNamespace, versionInfo,
                controllerRouteName, Controller);

                // handling exception logger call
                controllerNameWithVersion = ControllerNameWithVersion(controllerNameWithVersion);

                var type = Type.GetType(controllerNameWithVersion, true, true);
                var controllerDescriptor = new HttpControllerDescriptor(_config, controllerName, type);
                return controllerDescriptor;
            }
            catch (Exception)
            {
                // you should log exceptions here first with any avialble logger i.e. log4net, NLog etc
                // wrapping exception to custom Unauthorized request
                var httpError = new HttpError(GenericUnAuthorized);
                var errorResponse = request.CreateErrorResponse(HttpStatusCode.Unauthorized, httpError);
                throw new HttpResponseException(errorResponse);
            }
        }
        // ignore versioning for apis
        private static string ControllerNameWithVersion(string controllerNameWithVersion)
        {
            string[] ignoreList = { }; // any possible list of controllers you want to ignore versions for
            foreach (var name in ignoreList)
            {
                if (controllerNameWithVersion.Contains(name))
                {
                    controllerNameWithVersion = controllerNameWithVersion.Replace(".api", string.Empty);
                }
            }
            return controllerNameWithVersion;
        }

        private static int? ApiVersion(HttpRequestMessage request, int? apiVersion)
        {
            const string csvUrl = "csv/v";
            // handling form submit because form doesn't submit custom header
            if (request.RequestUri.AbsoluteUri.ToLower().Contains(csvUrl))
            {
                var actionParts = request.RequestUri.AbsoluteUri.ToLower().Split(new[] { csvUrl }, StringSplitOptions.RemoveEmptyEntries);
                int version;
                if (Int32.TryParse(actionParts[1].Substring(0, 1), out version))
                {
                    apiVersion = version;
                }
            }
            return apiVersion;
        }

        private static string GetControllerNameFromRequest(HttpRequestMessage request)
        {
            var routeData = request.GetRouteData();
            // Look up controller in route data
            object controllerName;
            routeData.Values.TryGetValue(Controller.ToLower(), out controllerName);
            if (controllerName != null) return controllerName.ToString();
            return string.Empty;
        }
    }
}