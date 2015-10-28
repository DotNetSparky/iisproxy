using System;
using System.IO;
using System.Net;
using System.Web;
using JetBrains.Annotations;

namespace IisProxy.WebProxy
{
	public class ProxyResponseStream
	{
		protected HttpResponse Response { get; private set; }

		public ProxyResponseStream([NotNull] HttpResponse response)
		{
			Response = response;
		}

		public virtual void ProcessResponse(WebProxy proxy, HttpWebResponse httpResponse)
		{
			// send "as-is"
			Response.ClearHeaders();
			Response.Clear();
			Response.ContentType = httpResponse.ContentType;

			string s = httpResponse.CharacterSet;
			if (!string.IsNullOrEmpty(s))
				Response.Charset = s;

			using (Stream responseStream = httpResponse.GetResponseStream())
			{
				if (responseStream != null)
					responseStream.CopyTo(Response.OutputStream);
			}
			Response.Flush();
			Response.End();
		}
	}
}