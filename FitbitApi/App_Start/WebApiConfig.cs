using System.Web.Http;

namespace FitbitApi.App_Start
{
    public class WebApiConfig
    {
        public static void Configure(HttpConfiguration config)
        {
            config.MapHttpAttributeRoutes();

            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "api/{controller}/{id}",
                defaults: new { id = RouteParameter.Optional }
            );

            // This will prevent any xml formatting for responses. If we end up needing that in the future there are other workarounds
            config.Formatters.Remove(config.Formatters.XmlFormatter);
        }
    }
}