using FoxOne._3VJ.DataSource;
using FoxOne.Business;
using FoxOne.Business.Security;
using FoxOne.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;

namespace FoxOne.Web.Controllers
{
    public class SSOController : BaseController
    {
        public const string SSO_COOKIE_KEY = "SSOFromAppId";

        [CustomUnAuthorize]
        public ActionResult Index(int id)
        {
            if (new SSOService().Get(id) == null)
            {
                throw new PageNotFoundException();
            }
            if(User.Identity.IsAuthenticated)
            {
                var redirectUrl = new SSOService().GenerateToken(id);
                Logger.Info("System:单点登录，检测到用户已登陆OA，正在跳转到地址：{0}".FormatTo(redirectUrl));
                return Redirect(redirectUrl);
            }
            Response.Cookies.Add(new HttpCookie(SSO_COOKIE_KEY) { Value = id.ToString(), Expires = DateTime.Now.AddMinutes(2) });
            return RedirectToAction("LogOn", "Home");
        }

        [CustomUnAuthorize]
        public ActionResult Redirect()
        {
            if(Request.Cookies[SSO_COOKIE_KEY] !=null)
            {
                string tempId = Request.Cookies[SSO_COOKIE_KEY].Value;
                int id = 0;
                if(int.TryParse(tempId,out id))
                {
                    Response.Cookies[SSO_COOKIE_KEY].Expires = DateTime.Now.AddDays(-1);
                    return RedirectTo(id);
                }
            }
            throw new PageNotFoundException();
        }

        public ActionResult RedirectTo(int id)
        {
            if (new SSOService().Get(id) == null)
            {
                throw new PageNotFoundException();
            }
            var redirectUrl = new SSOService().GenerateToken(id);
            Logger.Info("System:单点登录，正在跳转到地址：{0}".FormatTo(redirectUrl));
            return Redirect(redirectUrl);
        }

        public ActionResult LogOut(int id)
        {
            if (new SSOService().Get(id) == null)
            {
                throw new PageNotFoundException();
            }
            if (User.Identity.IsAuthenticated)
            {
                Logger.Info("System:{0}:【{1}】注销，IP：{2}", Sec.User.Id, Sec.User.Name, Utility.GetWebClientIp());
                Sec.Provider.Abandon();
                FormsAuthentication.SignOut();
            }
            Response.Cookies.Add(new HttpCookie(SSO_COOKIE_KEY) { Value = id.ToString(), Expires = DateTime.Now.AddMinutes(2) });
            return RedirectToAction("LogOn", "Home");
        }
    }
}