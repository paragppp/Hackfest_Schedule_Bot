using Autofac;
using System.Web.Http;
using System.Configuration;
using Microsoft.ApplicationInsights.Extensibility;
using SampleAADv2Bot.Services;
using System.Reflection;
using Autofac.Integration.WebApi;

namespace SampleAADv2Bot
{
    public class WebApiApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            GlobalConfiguration.Configure(WebApiConfig.Register);

            var containerBuilder = new ContainerBuilder();


            // Get your HttpConfiguration.
            var config = GlobalConfiguration.Configuration;

            // Register your Web API controllers.
            containerBuilder.RegisterApiControllers(Assembly.GetExecutingAssembly());

            // OPTIONAL: Register the Autofac filter provider.
            containerBuilder.RegisterWebApiFilterProvider(config);

            // OPTIONAL: Register the Autofac model binder provider.
            containerBuilder.RegisterWebApiModelBinderProvider();

            // Set the dependency resolver to be Autofac.


            containerBuilder.Register<ILoggingService>(b => new LoggingService());
            containerBuilder.Register<IHttpService>(b => new HttpService(b.Resolve<ILoggingService>()));
            containerBuilder.Register<IRoomService>(b => new RoomService(b.Resolve<ILoggingService>()));
            containerBuilder.Register<IMeetingService>(b => new MeetingService(b.Resolve<IHttpService>(), b.Resolve<IRoomService>(), b.Resolve<ILoggingService>()));

            containerBuilder.Register(b => new MessagesController(b.Resolve<IMeetingService>(), b.Resolve<IRoomService>(), b.Resolve<ILoggingService>()));

            var container = containerBuilder.Build();
            config.DependencyResolver = new AutofacWebApiDependencyResolver(container);

            TelemetryConfiguration.Active.InstrumentationKey = ConfigurationManager.AppSettings["AppInsightsKey"];
        }
    }
}
