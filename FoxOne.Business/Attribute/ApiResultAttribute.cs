using FoxOne.Core;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web;
using System.Web.Http;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;
using System.Web.Mvc;

namespace FoxOne.Business
{
    public class NoPackageResultAttribute : System.Web.Http.Filters.ActionFilterAttribute
    {

    }

    public class ApiResultAttribute : System.Web.Http.Filters.ActionFilterAttribute
    {
        public override void OnActionExecuted(HttpActionExecutedContext actionExecutedContext)
        {
            base.OnActionExecuted(actionExecutedContext);

            if (actionExecutedContext.Exception != null)
            {
                return;
            }
            var noPackage = actionExecutedContext.ActionContext.ActionDescriptor.GetCustomAttributes<NoPackageResultAttribute>();
            if (!noPackage.Any())
            {
                var result = new ApiResultModel();
                result.Code = actionExecutedContext.ActionContext.Response.StatusCode;
                if (actionExecutedContext.ActionContext.Response.Content != null)
                {
                    var a = actionExecutedContext.ActionContext.Response.Content.ReadAsAsync<object>();
                    if (!a.IsFaulted)
                    {
                        result.Result = actionExecutedContext.ActionContext.Response.Content.ReadAsAsync<object>().Result;
                    }
                }
                result.Success = actionExecutedContext.ActionContext.Response.IsSuccessStatusCode;
                actionExecutedContext.Response =  actionExecutedContext.Request.CreateResponse(result.Code, result); ;
            }
        }
    }


    public class ApiErrorHandleAttribute : ExceptionFilterAttribute
    {
        public override void OnException(HttpActionExecutedContext actionExecutedContext)
        {
            base.OnException(actionExecutedContext);
            var error = actionExecutedContext.Exception;
            var errorMessage = error.Message;
            Logger.Error(errorMessage, error);
            var result = new ApiResultModel()
            {
                Code = HttpStatusCode.InternalServerError,
                ErrorMessage = errorMessage
            };
            actionExecutedContext.Response = actionExecutedContext.Request.CreateResponse(result.Code, result);
        }
    }

    public class ApiResultModel
    {
        public HttpStatusCode Code { get; set; }

        public bool Success { get; set; }

        public object Result { get; set; }

        public string ErrorMessage { get; set; }
    }

    public class CustomApiAuthorizeAttribute: System.Web.Http.AuthorizeAttribute
    {
        public override void OnAuthorization(HttpActionContext actionContext)
        {
            base.OnAuthorization(actionContext);
        }

        protected override void HandleUnauthorizedRequest(HttpActionContext actionContext)
        {
            var result = new ApiResultModel()
            {
                Code = HttpStatusCode.Forbidden,
                ErrorMessage = "没有权限"
            };
            actionContext.Response = actionContext.Request.CreateResponse(result.Code, result);
        }

        protected override bool IsAuthorized(HttpActionContext actionContext)
        {
            HttpContextBase context = (HttpContextBase)actionContext.Request.Properties["MS_HttpContext"];
            HttpRequestBase request = context.Request;
            var ip = request.ServerVariables["REMOTE_ADDR"];
            string[] allIp = ConfigurationManager.AppSettings["AllIp"].Split(',');
            if(allIp.Contains(ip))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }
    
}
