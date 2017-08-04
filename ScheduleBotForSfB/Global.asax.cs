using System.Web.Http;
using System.Configuration;
using Microsoft.ApplicationInsights.Extensibility;

namespace SampleAADv2Bot
{
    public class WebApiApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            GlobalConfiguration.Configure(WebApiConfig.Register);
            TelemetryConfiguration.Active.InstrumentationKey = ConfigurationManager.AppSettings["AppInsightsKey"];
        }
    }
}
