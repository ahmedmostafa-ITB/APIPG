using System.Configuration;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Cors;
using System.Web.Http.Routing;
using System.Net.Http.Headers;

namespace SelfServiceAPI
{
    public static class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {
            config.EnableCors();
           // string PublishedAPIURL = ConfigurationManager.AppSettings["PublishedAPIURL"];
           // var cors = new EnableCorsAttribute(PublishedAPIURL, "*", "*");

            //  var cors = new EnableCorsAttribute("*", "*", "*");

            //config.EnableCors(cors);

            //var constraints = new { httpMethod = new HttpMethodConstraint(HttpMethod.Options) };
            //config.Routes.IgnoreRoute("OPTIONS", "*pathInfo", constraints);


            // Web API routes
            config.MapHttpAttributeRoutes();

            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "api/{controller}/{action}/{id}",
                defaults: new { id = RouteParameter.Optional } 
            );
            // config.Formatters.XmlFormatter.SupportedMediaTypes.Add(MediaTypeHeaderValue("multipart/form-data"));
            config.Formatters.JsonFormatter.SupportedMediaTypes.Add((new MediaTypeHeaderValue("text/html")));


           //string url = ConfigurationManager.AppSettings["ApplicationLink"];
          //  config.EnableCors(new EnableCorsAttribute("*", "*", "*"));
        }
    }
}
