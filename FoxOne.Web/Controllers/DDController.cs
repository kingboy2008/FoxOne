using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using FoxOne.Business;
using FoxOne.Core;
using System.Web.Security;
using FoxOne._3VJ;
using FoxOne.Data;
using FoxOne.Business.Security;
using FoxOne.Business.DDSDK;
using FoxOne.Business.DDSDK.Entity;
using FoxOne.Business.DDSDK.Service;

namespace FoxOne.Web.Controllers
{
    public class DDController : BaseController
    {
        [CustomUnAuthorize]
        public ActionResult Index()
        {
            var meetingroomList = DBContext<DataDictionary>.Instance.FirstOrDefault(o => o.Code.Equals("MeetingRoom", StringComparison.OrdinalIgnoreCase)).Items;
            var meetingroomBook = Dao.Get().Query<MeetingRoomBookEntity>().ToList();
            var i = DateTime.Now;
            var bookInfo = meetingroomBook.Where(o => (TimeInRange(i, o.BeginTime) || TimeInRange(i, o.EndTime)));
            var info = meetingroomList.Select(o => new MeetingRoomBookInfoView()
            {
                Name = o.Name,
                Code = o.Code,
                BookInfo = bookInfo.Where(j => j.MeetingRoomId == o.Code).OrderBy(j => j.BeginTime).Select(k => DBContext<IUser>.Instance.Get(k.BookUserId).Name + "（" + k.BeginTime.ToString("HH: mm") + "至" + k.EndTime.ToString("HH: mm") + "）"
            ).ToList()
            }).ToList();
            ViewData["MeetingRoomBook"] = info;
            return View();
        }

        private bool TimeInRange(DateTime i, DateTime bookTime)
        {
            var rangeStart = new DateTime(i.Year, i.Month, i.Day, 6, 0, 0);
            var rangeEnd = new DateTime(i.Year, i.Month, i.Day, 23, 59, 0);
            return bookTime > rangeStart && bookTime < rangeEnd;
        }

        [CustomUnAuthorize]
        public ActionResult List()
        {
            return View();
        }

        [CustomUnAuthorize]
        public JsonResult Get(string id)
        {
            var user = DDHelper.GetUserInfo(id);
            if (user.IsNotNullOrEmpty())
            {
                var sysUser = DBContext<IUser>.Instance.FirstOrDefault(o => o.Code.Equals(user, StringComparison.OrdinalIgnoreCase));
                if (sysUser != null)
                {
                    FormsAuthentication.SetAuthCookie(sysUser.LoginId, true);
                    return Json(sysUser, JsonRequestBehavior.AllowGet);
                }
                else
                {
                    throw new FoxOneException("CellPhone_Not_Exist");
                }
            }
            throw new FoxOneException("User_Not_Exist");
        }

        /// <summary>
        /// 修复用户钉钉号与实际不一致
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        public ActionResult FixUserDDCode(string userId)
        {
            var user = DBContext<IUser>.Instance.FirstOrDefault(c => c.Id.Equals(userId, StringComparison.InvariantCultureIgnoreCase));
            var ddUsers = DDUserService.Get(user.Department.Code.ConvertTo<int>());
            var ddUser = ddUsers.FirstOrDefault(c => c.name.Equals(user.Name) && c.mobile.Equals(user.MobilePhone));
            if (ddUser == null)
            {
                throw new FoxOneException($"在{user.Department.Name}中无法找到{user.Name}({user.MobilePhone})的对应信息");
            }
            if (user.Code == ddUser.userid)
            {
                throw new FoxOneException("钉钉信息一致，无需改动");
            }
            user.Code = ddUser.userid;
            DBContext<IUser>.Update(user);
            return Json(true);
        }


        public JsonResult Sync()
        {
            Dao.Get().CreateTable<DDDepartmentInfo>(true);
            Dao.Get().CreateTable<DDUserInfo>(true);
            var depts = DDDepartmentService.Get();
            foreach (var dept in depts)
            {
                Dao.Get().Insert(dept);
                var users = DDUserService.Get(dept.id);
                foreach (var user in users)
                {
                    Dao.Get().Insert(user);
                }
            }
            return Json(true,JsonRequestBehavior.AllowGet);
        }

        public JsonResult SyncCurrentUser()
        {
            string id = Sec.User.Code;
            var ddUser = DDUserService.Get(id);
            var user = DBContext<IUser>.Instance.FirstOrDefault(o => o.Code == id);
            if (ddUser.avatar.IsNotNullOrEmpty() && (user.Avatar.IsNullOrEmpty() || (user.Avatar != null && !user.Avatar.Equals(ddUser.avatar, StringComparison.OrdinalIgnoreCase))))
            {
                user.Avatar = ddUser.avatar;
                Dao.Get().Update(user);
                DBContext<IUser>.ClearCache();
            }
            return Json(true);
        }

        /// <summary>
        /// 同步用户钉钉
        /// </summary>
        public JsonResult SyncUserDingDing(string userId)
        {
            var user = DBContext<IUser>.Instance.FirstOrDefault(c => c.Id.Equals(userId, StringComparison.InvariantCultureIgnoreCase));
            if (SysConfig.IsProductEnv)
            {
                if (user.Status.Equals(DefaultStatus.Enabled.ToString(), StringComparison.InvariantCultureIgnoreCase))
                {
                    var ddUsers = DDUserService.Get(user.Department.Code.ConvertTo<int>());
                    var ddUser = ddUsers.FirstOrDefault(c =>c!=null&&c.name.IsNotNullOrEmpty()&&c.mobile.IsNotNullOrEmpty()&& c.name.Equals(user.Name) && c.mobile.Equals(user.MobilePhone));
                    var newDDUser = new DDUserCreateInfo { mobile = user.MobilePhone, name = user.Name, department = new int[] { user.Department.Code.ConvertTo<int>() }, email = user.Mail };
                    if (ddUser == null)
                    {
                        //throw new FoxOneException($"在{user.Department.Name}中无法找到{user.Name}({user.MobilePhone})的对应信息");
                        if (user.Code.IsNullOrEmpty())//无dingdingID,钉钉无法从对应部门获取人员，只能视为未建立钉钉用户，需要新建
                        {
                            string code= DDUserService.Create(newDDUser);
                            if (code.IsNotNullOrEmpty())
                            {
                                user.Code = code;
                                DBContext<IUser>.Update(user);
                            }
                            else
                            {
                                throw new FoxOneException($"在{user.Department.Name}中无法找到{user.Name}({user.MobilePhone})的对应信息，尝试创建并创建失败");
                            }
                        }
                        else//钉钉对应部门无用户，但本地有钉钉信息，可能是换了部门，更新钉钉信息
                        {
                            newDDUser.userid = user.Code;
                            DDUserService.Update(newDDUser);
                        }
                    }
                    else if (user.Code == ddUser.userid)//常规统一更新
                    {
                        newDDUser.userid = user.Code;
                        try
                        {
                            DDUserService.Update(newDDUser);
                        }
                        catch(Exception ex)
                        {
                            throw new Exception("钉钉调用出错：" + ex.Message);
                        }
                    }

                    //if (user.Code.IsNullOrWhiteSpace())
                    //{
                    //    var dduser = new DDUserCreateInfo { mobile = user.MobilePhone, name = user.Name, department = new int[] { user.Department.Code.ConvertTo<int>() }, email = user.Mail };
                    //    string code = DDUserService.Create(dduser);
                    //    if (code.IsNullOrWhiteSpace())
                    //    {
                    //        throw new FoxOneException("创建钉钉用户失败!");
                    //    }
                    //    else
                    //    {
                    //        user.Code = code;
                    //        DBContext<IUser>.Update(user);
                    //        return "创建钉钉用户成功";
                    //    }
                    //}
                    //else
                    //{
                    //    var getDDuser = DDUserService.Get(user.Code);
                    //    if (getDDuser != null)
                    //    {
                    //        var dduser = new DDUserCreateInfo { mobile = user.MobilePhone, name = user.Name, department = new int[] { user.Department.Code.ConvertTo<int>() }, userid = user.Code, email = user.Mail };
                    //        try
                    //        {
                    //            bool code = DDUserService.Update(dduser);
                    //            if (!code)
                    //                throw new FoxOneException("更新钉钉用户失败!");
                    //                //throw new FoxOneException("企业中的手机号码和登陆钉钉的手机号码不一致");
                    //            else
                    //                return "更新钉钉用户成功";
                    //        }
                    //        catch (Exception ex)
                    //        {
                    //            //if (ex.ToString().Contains("企业中的手机号码和登陆钉钉的手机号码不一致"))
                    //            //    SendMail(user);
                    //        }
                    //    }
                    //    else
                    //        throw new FoxOneException("用户钉钉号不一致，先同步钉钉号！");
                //}
                }
                else //Disable User
                {
                    if (user.Code.IsNotNullOrEmpty())
                    {
                        var dduser = DDUserService.Get(user.Code);
                        if (dduser != null)
                        {
                            if (DDUserService.Delete(user.Code))
                            {
                                throw new FoxOneException("删除该钉钉用户失败！");
                            }
                        }
                        else
                        {
                            throw new FoxOneException("钉钉不存在该用户");
                        }
                        //return "钉钉不存在该用户";
                    }
                    else
                    {
                        throw new FoxOneException( "用户表中用户钉钉号为空");
                    }
                }
            }
            return Json(true,JsonRequestBehavior.AllowGet);
        }


        /// <summary>
        /// 同步组织钉钉
        /// </summary>
        public JsonResult SyncDeptDingDing(string deptId)
        {
            var dept = DBContext<IDepartment>.Instance.FirstOrDefault(c => c.Id.Equals(deptId, StringComparison.InvariantCultureIgnoreCase));
            if (SysConfig.IsProductEnv)
            {
                if (dept.Status.Equals(DefaultStatus.Enabled.ToString(), StringComparison.InvariantCultureIgnoreCase))
                {
                    if (dept.Code.IsNullOrWhiteSpace())
                    {
                        var dd_dept = new DDDepartmentInfo() { id = DDHelper.GetDDDepartId(), name = dept.Name, parentid = dept.Parent.Code.ConvertTo<int>(), order = 100 };
                        var ddId = DDDepartmentService.Create(dd_dept);
                        if (!ddId.ToString().IsNullOrWhiteSpace())
                        {
                            dept.Code = ddId.ToString();
                            DBContext<IDepartment>.Update(dept);
                            //return "创建钉钉组织成功";
                        }
                        else
                        {
                            throw new FoxOneException("创建钉钉组织失败!");
                        }
                    }
                    else
                    {
                        var dd_dept = new DDDepartmentInfo() { name = dept.Name, parentid = dept.Parent.Code.ConvertTo<int>(), id = dept.Code.ConvertTo<int>() };
                        var ddId = DDDepartmentService.Update(dd_dept);
                        if (ddId.ToString().IsNullOrWhiteSpace())
                        {
                            throw new FoxOneException("更新钉钉组织失败！");
                        }
                        //else
                        //    return "更新钉钉组织成功";
                    }
                }
                else
                {
                    var getDDdept = DDDepartmentService.Get(dept.Code.ConvertTo<int>());
                    if (getDDdept != null)
                    {
                        Logger.Info($"删除钉钉组织返回：{ DDDepartmentService.Delete(dept.Code.ConvertTo<int>())}");
                    }
                    //else
                    //    return "钉钉中不存在该部门，无需删除";
                }
            }
            return Json(true,JsonRequestBehavior.AllowGet);
        }


    }
}