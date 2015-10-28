using System;
using System.Configuration;
using System.Web;

namespace IisProxy.WebProxy
{
	public class ProxyHttpHandler : IHttpHandler
	{
		public bool IsReusable
		{
			get
			{
				return true;
			}
		}

		public void ProcessRequest(HttpContext context)
		{
			string proxyAddress = context.Request.Url.PathAndQuery;

			if (!string.IsNullOrEmpty(proxyAddress))
			{
				var proxy = new WebProxy();
				proxy.BaseRewriteRemoteUrl = new Uri(ConfigurationManager.AppSettings["proxy-url"]);

				Uri proxyUrl;
				if (Uri.TryCreate(proxy.BaseRewriteRemoteUrl, proxyAddress, out proxyUrl))
				{
					proxy.NonHtmlResponseStream = new ProxyResponseStream(context.Response);
					//proxy.HtmlResponseStream = new HtmlContentFilteringResponseStream(proxy, context.Response);

					try
					{
						proxy.GetProxyContent(context.Request, proxyUrl);
					}
					catch (WebProxyException ex)
					{
						context.Trace.Warn("ProxyContent", "WebProxyException: " + ex.Message);
						if (ex.Url != null)
							context.Trace.Warn("ProxyContent", "Url: " + ex.Url);

						context.Response.Write("<p>Sorry, the information you are trying to access is not available at this time. Please try again later.</p>");
						return;
					}

#if DEBUG
					context.Trace.Warn("ProxyContent", "Successful; Http Status Code: " + ((int) proxy.HttpStatusCode) + " " + proxy.HttpStatusMessage);
#endif
				}
			}
		}
	}
}