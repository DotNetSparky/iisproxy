using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Routing;

namespace IisProxy
{
	public class RegexRoute : Route
	{
		readonly Regex _urlPattern;
		readonly string _patternValue;

		public string UrlPattern
		{
			get
			{
				return _patternValue;
			}
		}

		public RegexRoute(string urlPattern, IRouteHandler routeHandler)
			: this(urlPattern, null, routeHandler)
		{
		}

		public RegexRoute(string urlPattern, RouteValueDictionary defaults, IRouteHandler routeHandler)
			: this(urlPattern, defaults, null, routeHandler)
		{
		}

		public RegexRoute(string urlPattern, RouteValueDictionary defaults, RouteValueDictionary constraints, IRouteHandler routeHandler)
			: this(urlPattern, defaults, constraints, null, routeHandler)
		{
		}

		public RegexRoute(string urlPattern, RouteValueDictionary defaults, RouteValueDictionary constraints, RouteValueDictionary dataTokens, IRouteHandler routeHandler)
			: base(null, defaults, constraints, dataTokens, routeHandler)
		{
			_patternValue = urlPattern;
			_urlPattern = new Regex(urlPattern, RegexOptions.Compiled);
		}

		public override RouteData GetRouteData(HttpContextBase httpContext)
		{
			string requestUrl = httpContext.Request.AppRelativeCurrentExecutionFilePath.Substring(2) + httpContext.Request.PathInfo;
			Match match = _urlPattern.Match(requestUrl);
			RouteData data = null;
			if (match.Success)
			{
				data = new RouteData(this, RouteHandler);
				// add defaults first
				if (Defaults != null)
				{
					foreach (KeyValuePair<string, object> def in Defaults)
					{
						data.Values[def.Key] = def.Value;
					}
				}
				// then add values from request
				for (int i = 1; i < match.Groups.Count; i++)
				{
					Group group = match.Groups[i];
					if (group.Success)
					{
						string key = _urlPattern.GroupNameFromNumber(i);
						if (!string.IsNullOrEmpty(key) && !char.IsNumber(key, 0))
							data.Values[key] = group.Value;
					}
				}
				// check constraints
				if (Constraints != null)
				{
					if (Constraints.Any(i => !ProcessConstraint(httpContext, i.Value, i.Key, data.Values, RouteDirection.IncomingRequest)))
						return null;
				}
				// add data tokens
				if (DataTokens != null)
				{
					foreach (KeyValuePair<string, object> i in DataTokens)
					{
						data.DataTokens[i.Key] = i.Value;
					}
				}
			}
			return data;
		}

		public override VirtualPathData GetVirtualPath(RequestContext requestContext, RouteValueDictionary values)
		{
			return null;
		}
	}
}