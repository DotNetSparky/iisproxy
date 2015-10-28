using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Security;
using System.Web;
using JetBrains.Annotations;

namespace IisProxy.WebProxy
{
	public class WebProxy
	{
		const string DefaultUserAgent = "Mozilla/4.0 (compatible; MSIE 7.0; Windows NT 5.1; Trident/4.0) OnEdgePerformance.com WebProxy v1";
		const int DefaultTimeoutSeconds = 30;

		readonly List<string> _excludeLinks;

		public string UserAgent { get; set; }
		public TimeSpan Timeout { get; set; }
		public string LimitHost { get; set; }
		public Uri BaseRewriteUrl { get; set; }
		public Uri BaseRewriteRemoteUrl { get; set; }
		public Dictionary<string,string> RewriteLinkMap { get; set; }
		public bool DebugHtmlRendering { get; set; }
		public HttpStatusCode HttpStatusCode { get; protected set; }
		public string HttpStatusMessage { get; protected set; }
		public string ContentType { get; protected set; }
		public long ContentLength { get; protected set; }
		public ProxyResponseStream NonHtmlResponseStream { get; set; }
		public ProxyResponseStream HtmlResponseStream { get; set; }
		public Uri SourceUrl { get; private set; }

		[NotNull]
		public List<string> ExcludeLinks
		{
			get
			{
				return _excludeLinks;
			}
		}

		public WebProxy()
		{
			_excludeLinks = new List<string>();

			UserAgent = DefaultUserAgent;
			Timeout = TimeSpan.FromSeconds(DefaultTimeoutSeconds);
		}

		public virtual Uri UnwrapUrlFromProxyUrl([NotNull] Uri url)
		{
			if (BaseRewriteRemoteUrl != null && BaseRewriteUrl != null && url.IsAbsoluteUri && BaseRewriteUrl.IsBaseOf(url))
			{
				Uri relativeUrl = BaseRewriteUrl.MakeRelativeUri(url);
				if (!relativeUrl.IsAbsoluteUri)
				{
					HttpContext.Current.Trace.Warn("UnwrapUrlFromProxyUrl", "Original: " + url + ", relative: " + relativeUrl);
					Uri newUrl;
					if (Uri.TryCreate(BaseRewriteRemoteUrl, relativeUrl, out newUrl))
						return newUrl;
				}
			}
			return url;
		}

		public virtual Uri WrapUrlIntoProxyUrl([NotNull] Uri url)
		{
			if (BaseRewriteRemoteUrl != null && BaseRewriteUrl != null && url.IsAbsoluteUri && BaseRewriteRemoteUrl.IsBaseOf(url))
			{
				// fix any "//" in the path, these screw up the relative calculations
				string path = url.PathAndQuery;
				if (path.IndexOf("//", StringComparison.Ordinal) > -1)
				{
					path = path.Replace("//", "/");
					url = new Uri(url, path);
				}

				Uri relativeUrl = BaseRewriteRemoteUrl.MakeRelativeUri(url);

				if (!relativeUrl.IsAbsoluteUri)
				{
					HttpContext.Current.Trace.Warn("WrapUrlFromProxyUrl", "Original: " + url + ", relative: " + relativeUrl);
					Uri newUrl;
					if (Uri.TryCreate(BaseRewriteUrl, relativeUrl, out newUrl))
					{
						HttpContext.Current.Trace.Warn("New url: " + newUrl);
						return newUrl;
					}
				}
			}
			return url;
		}

		public virtual bool ValidateUrlAllowedForProxy([NotNull] Uri url)
		{
			return string.IsNullOrEmpty(LimitHost) || url.Host.Equals(LimitHost, StringComparison.OrdinalIgnoreCase);
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="sourceUrl"></param>
		/// <returns></returns>
		/// <exception cref="NotSupportedException">The scheme in the url is not supported.</exception>
		/// <exception cref="SecurityException">The caller does not have permission to connect to the requested url.</exception>
		protected virtual HttpWebRequest CreateProxyRequest(Uri sourceUrl)
		{
			var proxyRequest = (HttpWebRequest) WebRequest.Create(sourceUrl);
			proxyRequest.AllowAutoRedirect = true;
			proxyRequest.UserAgent = UserAgent;
			proxyRequest.Timeout = (int) Timeout.TotalMilliseconds;
			return proxyRequest;
		}

		public void GetProxyContent([CanBeNull] HttpRequest mimicRequest, [NotNull] Uri rewriteUrl)
		{
			try
			{
				HttpStatusCode = 0;
				HttpStatusMessage = string.Empty;

				SourceUrl = rewriteUrl;
				ProcessProxyRequest(mimicRequest);
			}
			catch (SecurityException ex)
			{
				throw new WebProxyException(rewriteUrl, ex.Message, ex);
			}
			catch (WebException ex)
			{
				throw new WebProxyException(rewriteUrl, ex.Message, ex);
			}
			catch (IOException ex)
			{
				throw new WebProxyException(rewriteUrl, ex.Message, ex);
			}
		}

		protected virtual void ProcessProxyRequest([CanBeNull] HttpRequest mimicRequest)
		{
			HttpWebRequest request = CreateProxyRequest(SourceUrl);
			if (mimicRequest != null)
			{
				request.Method = mimicRequest.HttpMethod;
				request.ContentLength = mimicRequest.ContentLength;
				request.ContentType = mimicRequest.ContentType;
				if (request.Method == "PUT" || request.Method == "POST")
				{
					using (Stream content = mimicRequest.InputStream)
					{
						using (Stream requestStream = request.GetRequestStream())
						{
							content.CopyTo(requestStream);
							requestStream.Flush();
						}
					}
				}
				string originHeader = mimicRequest.Headers["Origin"];
				if (originHeader != null)
					request.Headers["Origin"] = originHeader;
				Uri referrer = mimicRequest.UrlReferrer;
				if (referrer != null)
					request.Referer = referrer.ToString();
			}

			using (WebResponse httpResponse = request.GetResponse())
			{
				var webResponse = (HttpWebResponse) httpResponse;
				HttpStatusCode = webResponse.StatusCode;
				HttpStatusMessage = webResponse.StatusDescription;

				if (webResponse.StatusCode != HttpStatusCode.OK)
					throw new WebProxyException(SourceUrl, (int) webResponse.StatusCode, webResponse.StatusDescription);

				ContentType = webResponse.ContentType;
				ContentLength = webResponse.ContentLength;

//				if (NonHtmlResponseStream != null && !webResponse.ContentType.StartsWith("text", StringComparison.OrdinalIgnoreCase))
//				{
//					// send "as-is"
					NonHtmlResponseStream.ProcessResponse(this, webResponse);
//				}
//				else if (HtmlResponseStream != null)
//					HtmlResponseStream.ProcessResponse(this, webResponse);
			}
		}
	}
}