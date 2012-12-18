using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using Microsoft.AspNet.Membership.OpenAuth;
using N2.Engine;
using N2.Templates.Mvc.Controllers;
using N2.Web.Mvc;
using N2.Templates.Mvc.Web;

namespace N2.Templates.Mvc
{
	// Note: For instructions on enabling IIS6 or IIS7 classic mode, 
	// visit http://go.microsoft.com/?LinkId=9394801

	public class MvcApplication : HttpApplication
	{
		protected void Application_Start()
		{
			var cmsEngine = N2.Context.Initialize(false);

			RegisterControllerFactory(ControllerBuilder.Current, cmsEngine);

			RegisterViewEngines(ViewEngines.Engines);

			AreaRegistration.RegisterAllAreas(new AreaRegistrationState(cmsEngine));

			RegisterRoutes(RouteTable.Routes, cmsEngine);

		  RegisterOpenAuth();
		}

    public static void RegisterOpenAuth()
    {
      // See http://go.microsoft.com/fwlink/?LinkId=252803 for details on setting up this ASP.NET
      // application to support logging in via external services.

      //OpenAuth.AuthenticationClients.AddTwitter(
      //    consumerKey: "your Twitter consumer key",
      //    consumerSecret: "your Twitter consumer secret");

      //OpenAuth.AuthenticationClients.AddFacebook(
      //    appId: "your Facebook app id",
      //    appSecret: "your Facebook app secret");

      //OpenAuth.AuthenticationClients.AddMicrosoft(
      //    clientId: "your Microsoft account client id",
      //    clientSecret: "your Microsoft account client secret");

      OpenAuth.AuthenticationClients.AddGoogle();
    }

		public void RegisterControllerFactory(ControllerBuilder controllerBuilder, IEngine engine)
		{
			engine.RegisterAllControllers();

			var controllerFactory = engine.Resolve<ControllerFactoryConfigurator>()
				.NotFound<StartController>(sc => sc.NotFound())
				.ControllerFactory;

			controllerBuilder.SetControllerFactory(controllerFactory);
		}

		public static void RegisterViewEngines(ViewEngineCollection viewEngines)
		{
			viewEngines.Insert(0, new ThemedMasterViewEngine());
		}

		public static void RegisterRoutes(RouteCollection routes, IEngine engine)
		{
			routes.MapContentRoute("Content", engine);

			routes.MapRoute(
				"Default", // Route name
				"{controller}/{action}/{*id}", // URL with parameters
                new { action = "Index", id = UrlParameter.Optional } // Parameter defaults
				);
		}
	}
}