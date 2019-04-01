using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using System.Web.Http;
using FoxOne.Business;

namespace FoxOne.Web
{

    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();

            WebApiRegister(GlobalConfiguration.Configuration);
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
            RegisterCenter.RegisterType();
            RegisterCenter.RegisterEntityEvent();

            DiscoveryClientManager.InitializeAndStart();
        }

        protected async void Application_End()
        {
            DiscoveryClientManager.Shutdown();
        }

        public static void WebApiRegister(HttpConfiguration config)
        {
            config.Filters.Add(new ApiResultAttribute());
            config.Filters.Add(new ApiErrorHandleAttribute());
            config.Filters.Add(new Controllers.RequestHeaderLogFilterAttribute());
            config.Routes.MapHttpRoute(name: "User", routeTemplate: "api/{controller}/{action}");
        }

        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");
            routes.MapRoute(
                name: "PagePreview",
                url: "Page/PreView",
                defaults: new { controller = "Page", action = "PreView" }
            );
            routes.MapRoute(
                name: "Page",
                url: "Page/{pageId}/{ctrlId}",
                defaults: new { controller = "Page", action = "Index", ctrlId = UrlParameter.Optional }
            );

            routes.MapRoute(
                name: "Entity",
                url: "Entity/{action}/{entityName}/{id}",
                defaults: new { controller = "Entity", action = "List", id = UrlParameter.Optional }
            );

            routes.MapRoute(
                name: "Default",
                url: "{controller}/{action}/{id}",
                defaults: new { controller = "Home", action = "LogOn", id = UrlParameter.Optional }
            );
        }
    }
}