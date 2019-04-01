using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Web;
using System.Web.Http;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;
using FoxOne.Core;

namespace FoxOne.Web.Controllers
{
    public class RequestHeaderLogFilterAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(HttpActionContext actionContext)
        {
            StringBuilder sb = new StringBuilder();
            var request = HttpContext.Current.Request;
            sb.AppendFormat("HTTP {2} :From {0} To {1} \r\n", request.UrlReferrer,request.Url,request.HttpMethod);
            sb.AppendLine("Header");
            foreach (string key in request.Headers)
            {
                sb.AppendFormat("\t {0}:{1}\r\n", key, request.Headers[key]);
            }
            sb.AppendLine("QueryString");
            foreach (string key in request.QueryString.Keys)
            {
                sb.AppendFormat("\t {0}:{1}\r\n", key, HttpContext.Current.Server.UrlDecode(request.QueryString[key]));
            }
            sb.AppendLine("Form");
            foreach (string key in request.Form)
            {
                sb.AppendFormat("\t {0}:{1}\r\n", key, request.Form[key]);
            }
            sb.AppendLine("Cookies");
            foreach (string key in request.Cookies)
            {
                sb.AppendFormat("\t {0}:{1}\r\n", key, request.Cookies[key]);
            }
            Logger.Info(sb.ToString());
        }
    }
}
