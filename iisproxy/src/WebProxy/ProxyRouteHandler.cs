using System.Web;
using System.Web.Routing;

namespace IisProxy.WebProxy
{
	public class ProxyRouteHandler : IRouteHandler
	{
		public IHttpHandler GetHttpHandler(RequestContext requestContext)
		{
			return new ProxyHttpHandler();
		}
	}
}