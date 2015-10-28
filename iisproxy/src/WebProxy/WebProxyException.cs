using System;
using System.Runtime.Serialization;
using System.Security.Permissions;
using JetBrains.Annotations;

namespace IisProxy.WebProxy
{
	[Serializable]
	public class WebProxyException : Exception
	{
		// changed to CanBeNull
		[CanBeNull]
		public Uri Url { get; set; }

		public int HttpStatusCode { get; set; }

		[CanBeNull]
		public string HttpStatusMessage { get; set; }

		[CanBeNull]
		public string UrlString
		{
			get
			{
				return Url != null ? Url.ToString() : string.Empty;
			}
			set
			{
				Url = value != null ? new Uri(value) : null;
			}
		}

		public WebProxyException()
		{
		}

		public WebProxyException(string message)
			: base(message)
		{
		}

		public WebProxyException(string message, Exception innerException)
			: base(message, innerException)
		{
		}

		public WebProxyException(Uri url)
		{
			Url = url;
		}

		public WebProxyException([NotNull] Uri url, string message)
			: base(message)
		{
			Url = url;
		}

		public WebProxyException(Uri url, string message, Exception innerException)
			: base(message, innerException)
		{
			Url = url;
		}

		public WebProxyException([NotNull] Uri url, int httpStatusCode, string httpStatusMessage)
			: base(string.Format("The proxy received {0} {1}.", httpStatusCode, httpStatusMessage))
		{
			Url = url;
			HttpStatusCode = httpStatusCode;
			HttpStatusMessage = httpStatusMessage;
		}

		[SecurityPermission(SecurityAction.Demand, SerializationFormatter = true)]
		WebProxyException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
			UrlString = info.GetString("Url");
			HttpStatusCode = info.GetInt32("HttpStatusCode");
			HttpStatusMessage = info.GetString("HttpStatusMessage");
		}

		[SecurityPermission(SecurityAction.Demand, SerializationFormatter = true)]
		public override void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			base.GetObjectData(info, context);
			info.AddValue("Url", UrlString);
			info.AddValue("HttpStatusCode", HttpStatusCode);
			info.AddValue("HttpStatusMessage", HttpStatusMessage);
		}
	}
}