using System;
using System.Configuration;
using System.Web;
using System.Web.Routing;
using IisProxy.WebProxy;

namespace IisProxy
{
	public class Global : HttpApplication
	{
		protected void Application_Start(object sender, EventArgs e)
		{
			var proxyHandler = new ProxyRouteHandler();

			RouteTable.Routes.RouteExistingFiles = true;

			string routePatterns = ConfigurationManager.AppSettings["proxy-patterns"];
			if (!string.IsNullOrEmpty(routePatterns))
			{
				foreach (string i in routePatterns.Split(';'))
				{
					RouteTable.Routes.Add(new RegexRoute(i, proxyHandler));
				}
			}
		}
	}
}