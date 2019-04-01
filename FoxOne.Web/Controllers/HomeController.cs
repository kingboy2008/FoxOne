using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using System.Web.Security;
using FoxOne.Data;
using FoxOne.Controls;
using FoxOne.Business;
using FoxOne.Core;
using FoxOne.Business.Security;
using FoxOne.Business.OAuth;
using FoxOne.Business.Environment;
using FoxOne.Workflow.Business;
using FoxOne._3VJ;

namespace FoxOne.Web.Controllers
{
    public class HomeController : BaseController
    {

        private const string NextSendTimeKey = "NextSendTime";
        private const string OAuthUserKey = "OAuthUser";
        private const string PhoneKey = "phone";
        private const string RequestState = "RequestState";
        private const string FailTimes = "FailTimes";
        public ActionResult Index()
        {
            if (SysConfig.IsProductEnv)
            {
                ViewData["UnReadMail"] = new MailUserService().GetUnReadMail(Sec.User.Mail);
            }
            ViewData["ReadList"] = WorkflowHelper.GetReadList(Sec.User.Id).Count(c => c.ItemStatus.Equals(Workflow.Kernel.WorkItemStatus.Sent.GetDescription()));
            ViewData["ToDoList"] =  WorkflowHelper.GetToDoList(Sec.User.Id).Count;
            return View();
        }

        public ActionResult GetToSendMail(string id)
        {
            if (SysConfig.IsProductEnv)
            {
                var sid = new MailUserService().UserLogin(Sec.User.Mail);
                if (sid.IsNullOrEmpty())
                {
                    throw new Exception("未开通邮箱！");
                }
                return Redirect($"http://mail.3vjia.com/coremail/XT5/index.jsp?sid={sid}#mail.compose|{{\"to\":\"{id}\",\"subject\":\"主题\"}}");
            }
            else
            {
                throw new PageNotFoundException();
            }
        }

        public ActionResult GoToMail()
        {
            if (SysConfig.IsProductEnv)
            {
                var sid = new MailUserService().UserLogin(Sec.User.Mail);
                if (sid.IsNullOrEmpty())
                {
                    throw new Exception("未开通邮箱！");
                }
                return Redirect($"http://mail.3vjia.com/coremail/main.jsp?sid={sid}");
            }
            else
            {
                throw new PageNotFoundException();
            }
        }

        public ActionResult GoToMobileMail()
        {
            if (SysConfig.IsProductEnv)
            {
                var sid = new MailUserService().UserLogin(Sec.User.Mail);
                if (sid.IsNullOrEmpty())
                {
                    throw new Exception("未开通邮箱！");
                }
                return Redirect($"http://mail.3vjia.com/coremail/xphone/main.jsp?sid={sid}");
            }
            else
            {
                throw new PageNotFoundException();
            }
        }

        private string GetSystemUrl(IPermission o, string url)
        {

            while (o.Type != PermissionType.System)
            {
                o = o.Parent;
            }
            if (o.Code == "OA")
            {
                return url;
            }
            else
            {
                var s = DBContext<SSOSystem>.Instance.FirstOrDefault(k => k.AgentId == o.Code.ConvertTo<int>());
                if (!s.HomeUrl.EndsWith("/"))
                {
                    s.HomeUrl = s.HomeUrl + "/";
                }
                if (url.StartsWith("/"))
                {
                    url = url.Substring(1);
                }
                return s.HomeUrl + url;
            }
        }

        public JsonResult GetMenu()
        {
            var temp = Sec.Provider.GetAllUserPermission().Where(o => o.Type < PermissionType.Control && o.Status.Equals(DefaultStatus.Enabled.ToString(), StringComparison.OrdinalIgnoreCase)).OrderBy(o => o.Rank);
            var result = new List<TreeNode>();
            temp.ForEach(o =>
            {
                result.Add(new TreeNode
                {
                    Value = o.Id,
                    ParentId = o.ParentId,
                    Text = o.Name,
                    Url = GetSystemUrl(o, Env.Parse(o.Url)),
                    Icon = o.Icon
                });
                if (o.Parent != null)
                {
                    if (result.Count(p => p.Value.Equals(o.Parent.Id, StringComparison.OrdinalIgnoreCase)) == 0)
                    {
                        result.Add(new TreeNode
                        {
                            Value = o.Parent.Id,
                            ParentId = string.Empty,
                            Text = o.Parent.Name,
                            Url = o.Parent.Url,
                            Icon = o.Parent.Icon
                        });
                    }
                }
            });
            return Json(result, JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetDataBySqlId(string sqlId, string type)
        {
            if (sqlId.StartsWith("crud", StringComparison.OrdinalIgnoreCase))
            {
                string[] temp = sqlId.Split('.');
                var entity = DBContext<CRUDEntity>.Instance.Get(temp[1]);
                switch (temp[2].ToUpper())
                {
                    case "INSERT":
                        sqlId = entity.InsertSQL;
                        break;
                    case "UPDATE":
                        sqlId = entity.UpdateSQL;
                        break;
                    case "DELETE":
                        sqlId = entity.DeleteSQL;
                        break;
                    case "SELECT":
                        sqlId = entity.SelectSQL;
                        break;
                    case "GET":
                        sqlId = entity.GetOneSQL;
                        break;
                }
            }
            else
            {
                if (DaoFactory.GetSqlSource().Find(sqlId) == null)
                {
                    throw new FoxOneException("SqlId_Not_Found", sqlId);
                }
            }
            if (type.Equals("exec:", StringComparison.OrdinalIgnoreCase))
            {
                return Json(Dao.Get().ExecuteNonQuery(sqlId, Request.Params.ToDictionary()) > 0, JsonRequestBehavior.AllowGet);
            }
            else
            {
                return Json(Dao.Get().QueryDictionaries(sqlId,Request.Params.ToDictionary()), JsonRequestBehavior.AllowGet);
            }
        }

        public JsonResult GetWidgetData()
        {
            string pageId = Request[NamingCenter.PARAM_PAGE_ID];
            string ctrlId = Request[NamingCenter.PARAM_CTRL_ID];
            string widgetType = Request[NamingCenter.PARAM_WIDGET_TYPE];
            if (ctrlId.IsNullOrEmpty())
            {
                throw new FoxOneException("Parameter_Not_Found", NamingCenter.PARAM_CTRL_ID);
            }
            var ctrl = PageBuilder.BuildPage(pageId).FindControl(ctrlId);
            if (ctrl == null)
            {
                throw new FoxOneException("Ctrl_Not_Found", ctrlId);
            }
            if (widgetType.Equals("Chart", StringComparison.OrdinalIgnoreCase))
            {
                var chart = ctrl as NoPagerListControlBase;
                if (chart == null)
                {
                    throw new FoxOneException("Need_To_Be_NoPagerListControlBase", chart.Id);
                }
                if (chart.DataSource == null)
                {
                    throw new FoxOneException("Need_DataSource", chart.Id);
                }
                return Json(chart.GetData(), JsonRequestBehavior.AllowGet);
            }
            else
            {
                var tree = ctrl as Tree;
                if (tree == null)
                {
                    throw new FoxOneException("Need_To_Be_Tree", tree.Id);
                }
                if (tree.DataSource == null)
                {
                    throw new FoxOneException("Need_DataSource", tree.Id);
                }
                return Json(tree.DataSource.SelectItems(), JsonRequestBehavior.AllowGet);

            }
        }

        #region OAuth验证
        [CustomUnAuthorize]
        public ActionResult QQLogOn()
        {
            AuthenticationScope scope = new AuthenticationScope()
            {
                State = Utility.GetGuid(),
                Scope = "get_user_info"
            };
            Session[RequestState] = scope.State;
            string url = GetAuthHandler("QQ").GetAuthorizationUrl(scope);
            return Redirect(url);
        }

        [CustomUnAuthorize]
        public ActionResult DDLogOn()
        {
            AuthenticationScope scope = new AuthenticationScope()
            {
                State = Utility.GetGuid(),
                Scope = "snsapi_login"
            };
            Session[RequestState] = scope.State;
            string url = GetAuthHandler("DD").GetAuthorizationUrl(scope);
            return Redirect(url);
        }

        [ValidateInput(false)]
        [CustomUnAuthorize]
        public ActionResult QQLogOnCallback()
        {
            return LogOnCallbackInner("QQ");
        }

        [ValidateInput(false)]
        [CustomUnAuthorize]
        public ActionResult DDLogOnCallback()
        {
            return LogOnCallbackInner("DD");
        }

        private ActionResult LogOnCallbackInner(string tag)
        {
            var verifier = Request.Params["code"];
            var verifierState = Request.Params["state"];
            string state = Session[RequestState] == null ? string.Empty : Session[RequestState].ToString();
            if (verifierState.IsNotNullOrEmpty() && verifierState.Equals(state, StringComparison.OrdinalIgnoreCase))
            {
                AuthenticationTicket ticket = new AuthenticationTicket()
                {
                    Code = verifier,
                    Tag = tag
                };
                var tencentHandler = GetAuthHandler(tag);
                ticket = tencentHandler.PreAuthorization(ticket);
                ticket = tencentHandler.AuthenticateCore(ticket);
                var user = tencentHandler.GetUserInfo(ticket);
                if (user != null)
                {
                    if (User.Identity.IsAuthenticated)
                    {
                        throw new Exception("该{0}号已被绑定。".FormatTo(tag));
                    }
                    else
                    {
                        if (user.Status.Equals(DefaultStatus.Disabled.ToString(), StringComparison.OrdinalIgnoreCase) || user.Department.Status.Equals(DefaultStatus.Disabled.ToString(), StringComparison.OrdinalIgnoreCase))
                        {
                            throw new Exception("用户或用户所属组织已被禁用");
                        }
                        Log(user, tag);
                        return ToHomePage(user);
                    }
                }
                else
                {
                    if (User.Identity.IsAuthenticated)
                    {
                        var userClaim = new UserClaim()
                        {
                            Id = Utility.GetGuid(),
                            LoginId = Sec.User.LoginId,
                            UserId = Sec.User.Id,
                            OpenId = ticket.OpenId,
                            Tag = ticket.Tag,
                            RentId = 1,
                            UnionId = ticket.UnionId,
                            Token = ticket.AccessToken
                        };
                        DBContext<UserClaim>.Insert(userClaim);
                        return RedirectToAction("UserBind", "Home");
                    }
                    else
                    {
                        throw new Exception("未绑定本地账号，请先用手机验证码登录后，在【我的登录方式】绑定当前的{0}号。".FormatTo(tag));
                    }
                }
            }
            return RedirectToAction("LogOn");
        }

        private AuthenticationHandler GetAuthHandler(string tag)
        {
            if (tag.Equals("QQ", StringComparison.OrdinalIgnoreCase))
            {
                var _options = new AuthenticationOptions()
                {
                    AppId = "101560471",
                    AppSecret = "0e59cd3f34abdfec1103aa5eb003ae00",
                    AuthorizeUrl = "https://graph.qq.com",
                    Host = "http://www.foxone.cc",
                    Callback = "/Home/QQLogOnCallback"
                };
                return new QQAuthenticationHandler(_options);
            }
            else
            {
                var _options = new AuthenticationOptions()
                {
                    AppId = "dingoa5zhpevbqhhcdtyrr",
                    AppSecret = "yiqa64BkKkS7cS2lk0ul2GjsCkna1muq3Yb_pdAI5gxWM4G57AoXjdUl3ZKtPQFY",
                    AuthorizeUrl = "https://oapi.dingtalk.com",
                    Host = "http://www.foxone.cc",
                    Callback = "/Home/DDLogOnCallback"
                };
                return new DingDingAuthenticationHandler(_options);
            }
        }


        public ActionResult UserBind()
        {
            return View();
        }

        #endregion

        [ValidateAntiForgeryToken]
        [HttpPost]
        [CustomUnAuthorize]
        public JsonResult GetValidCode()
        {
            if (Session[FailTimes] == null)
            {
                throw new FoxOneException("InValid_Operation");
            }
            string phone = Request.Form[PhoneKey];
            if (DBContext<IUser>.Instance.Count(o => o.Status.Equals("enabled", StringComparison.OrdinalIgnoreCase) && o.MobilePhone.IsNotNullOrEmpty() && o.MobilePhone.Equals(phone, StringComparison.OrdinalIgnoreCase)) == 0)
            {
                throw new FoxOneException("Phone_Not_Exist");
            }
            string key = phone + "_Phone_Valid_Code";

            if (CacheHelper.GetValue(key) != null)
            {
                throw new FoxOneException("InValid_Operation");
            }
            if (Session[NextSendTimeKey] != null)
            {
                var canNotGet = ((DateTime)Session[NextSendTimeKey]) > DateTime.Now;
                if (canNotGet)
                {
                    throw new FoxOneException("InValid_Operation");
                }
                else
                {
                    Session.Remove(NextSendTimeKey);
                }
            }
            string validCode = GetValidCodeString();
            var isSend = Swj.Sms.SuppliersHelper.MSMHelper.SendSMS(phone, validCode, Swj.Sms.SuppliersHelper.MSMType.ChuangLan, Swj.Sms.SuppliersHelper.MSType.Identifying);
            if (isSend)
            {
                DateTime expiredTime = DateTime.Now.AddMinutes(1);
                Dao.Get().Delete<UserValid>().Where(o => o.Phone == phone).Execute();
                Dao.Get().Insert<UserValid>(new UserValid()
                {
                    Id = Utility.GetGuid(),
                    Phone = phone,
                    ValidCode = validCode,
                    ExpiredTime = expiredTime
                });
                CacheHelper.SetValue(key, true, expiredTime, System.Web.Caching.Cache.NoSlidingExpiration);
                Session[NextSendTimeKey] = expiredTime;
            }
            return Json(new { Sended = isSend });
        }

        private string GetValidCodeString()
        {
            var random = new Random();
            var validCode = random.Next(1000, 9999).ToString();
            var letters = "ABCDEFGHJKLMNPQRSTUVWXYZ";
            System.Threading.Thread.Sleep(20);
            var i = new Random().Next(0, letters.Length - 1);
            System.Threading.Thread.Sleep(20);
            var j = new Random().Next(0, letters.Length - 1);
            return validCode = validCode.Insert(i % 5, letters[i].ToString()).Insert(j % 5, letters[j].ToString());
        }


        [CustomUnAuthorize]
        public ActionResult LogOn()
        {
            FormsAuthentication.SignOut();
            Sec.Provider.Abandon();
            if (Session[FailTimes] == null)
            {
                Session[FailTimes] = 0;
            }
            return View();

        }

        [CustomUnAuthorize]
        [HttpPost]
        public ActionResult LogOn(string userName, string password)
        {
            if (Sec.Provider.Authenticate(userName, password))
            {
                FormsAuthentication.SetAuthCookie(userName, false);
                var user = DBContext<IUser>.Instance.FirstOrDefault(o => o.LoginId.Equals(userName, StringComparison.OrdinalIgnoreCase));
                Log(user, "用户名密码");
                string returnUrl = Request.QueryString["ReturnUrl"];
                if (!returnUrl.IsNullOrEmpty())
                {
                    return Redirect(returnUrl);
                }
                else
                {
                    return RedirectToAction("Index", "Home");
                }
            }
            else
            {
                ViewData["ErrorMessage"] = "账号或密码错误！";
                return View();
            }
        }


        //[ValidateAntiForgeryToken]
        //[CustomUnAuthorize]
        //[HttpPost]
        //public ActionResult LogOn(string phone, string validCode)
        //{
        //    if (Session[FailTimes] == null || (Session[FailTimes] != null && ((int)Session[FailTimes]) > 5))
        //    {
        //        ViewData["ErrorMessage"] = ObjectHelper.GetObject<ILangProvider>().GetString("InValid_Operation");
        //        return View();
        //    }
        //    var userValid = Dao.Get().Query<UserValid>().Where(o => o.Phone == phone && o.ValidCode == validCode).ToList();
        //    if (userValid.IsNullOrEmpty() || userValid.First().ExpiredTime < DateTime.Now)
        //    {
        //        Session[FailTimes] = ((int)Session[FailTimes]) + 1;
        //        ViewData["ErrorMessage"] = ObjectHelper.GetObject<ILangProvider>().GetString("InValid_Phone_ValidCode");
        //        return View();
        //    }
        //    Dao.Get().Delete<UserValid>().Where(o => o.Phone == phone).Execute();
        //    var user = DBContext<IUser>.Instance.FirstOrDefault(o => o.MobilePhone.IsNotNullOrEmpty() && o.MobilePhone.Equals(phone, StringComparison.OrdinalIgnoreCase));
        //    Log(user, "手机验码证");
        //    Session.RemoveAll();
        //    return ToHomePage(user);
        //}

        private ActionResult ToHomePage(IUser user)
        {
            FormsAuthentication.SetAuthCookie(user.LoginId, false);
            if (Request.Cookies[SSOController.SSO_COOKIE_KEY] != null)
            {
                return RedirectToAction("Redirect", "SSO");
            }
            return RedirectToAction("Index", "Home");
        }

        private void Log(IUser user, string logType)
        {
            Logger.Info("System:{0}:【{1}】登录，IP：{2}，登录方式：{3}", user.Id, user.Name, Utility.GetWebClientIp(), logType);
        }

        //[CustomUnAuthorize]
        //[HttpPost]
        //public ActionResult LogOn(string userName, string password)
        //{
        //    if (Sec.Provider.Authenticate(userName, password))
        //    {
        //        FormsAuthentication.SetAuthCookie(userName, false);
        //        return RedirectToAction("Index", "Home");
        //    }
        //    else
        //    {
        //        ViewData["ErrorMessage"] = ObjectHelper.GetObject<ILangProvider>().GetString("InValid_User_Or_Password");
        //        return View();
        //    }
        //}


        [ValidateInput(false)]
        [CustomUnAuthorize]
        public ActionResult Error(string id)
        {
            ViewData["ErrorMessage"] = id;
            return View();
        }

        public ActionResult LogOut()
        {
            Logger.Info("System:{0}:【{1}】注销，IP：{2}", Sec.User.Id, Sec.User.Name, Utility.GetWebClientIp());
            Sec.Provider.Abandon();
            FormsAuthentication.SignOut();
            return RedirectToAction("LogOn");
        }

        public ActionResult ChangePassword()
        {
            return View();
        }

        [HttpPost]
        public JsonResult ChangePassword(FormCollection form)
        {
            string password = form["OldPassword"];
            string newPassword = form["NewPassword"];
            string confirmPassword = form["ConfirmPassword"];

            if (!newPassword.Equals(confirmPassword, StringComparison.OrdinalIgnoreCase))
            {
                throw new FoxOneException("NewPassword_NotEqual_ConfirmPassword");
            }
            if (Sec.Provider.Authenticate(Sec.User.LoginId, password))
            {
                if (Sec.Provider.ResetPassword(Sec.User.LoginId, newPassword))
                {
                    Logger.Info("System:{0}:【{1}】重置密码，IP：{2}，", Sec.User.Id, Sec.User.Name, Utility.GetWebClientIp());
                    return Json(true);
                }
            }
            else
            {
                throw new FoxOneException("Invalid_Original_Password");
            }
            return Json(false);
        }
    }
}
