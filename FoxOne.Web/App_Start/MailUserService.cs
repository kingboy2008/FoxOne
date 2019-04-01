using FoxOne.Core;
using FoxOne.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
namespace FoxOne.Web
{
    /// <summary>
    /// 邮箱用户信息
    /// </summary>
    public class MailUser
    {
        public string UserId { get; set; }

        public string DepartmentId { get; set; }


        public string Password { get; set; }

        public string Name { get; set; }

        public string Mobile { get; set; }
    }

    /// <summary>
    /// 邮箱部门信息
    /// </summary>
    public class MailDepartment
    {
        public string DepartmentId { get; set; }

        public string Name { get; set; }

        public string ParentId { get; set; }
    }

    /// <summary>
    /// 邮箱系统组织用户API
    /// </summary>
    public class MailUserService
    {
        private MailAPI.APIClient client = new MailAPI.APIClient();

        public const string Mail_Domain = "@3vjia.com";

        /// <summary>
        /// 新增用户
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        public bool CreateUser(MailUser user)
        {
            //user.UserId = GetValidUserId(user.UserId);
            user.UserId = user.UserId.Replace(Mail_Domain, "");
            user.Mobile = "1000";
            var result = client.createUser("1", "a", user.UserId, $"org_unit_id={user.DepartmentId}&user_status=0&domain_name=3vjia.com&cos_id=1&true_name={user.Name}&nick_name={user.Name}&mobile_number={user.Mobile}");
            if (result.code == 0)
            {
                return true;
            }
            throw new Exception(result.message);
        }

        /// <summary>
        /// 获取不重复的用户ID
        /// </summary>
        /// <param name="loginId"></param>
        /// <returns></returns>
        private string GetValidUserId(string loginId)
        {
            string result = loginId;
            int i = 1;
            while (client.userExist(result + Mail_Domain).code == 0)
            {
                result = loginId + i;
                i++;
            }
            return result;
        }

        /// <summary>
        /// 更新用户
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        public bool UpdateUser(MailUser user)
        {
            string userAtDomain = user.UserId;
            if (userAtDomain.IndexOf(Mail_Domain) < 0)
            {
                userAtDomain += Mail_Domain;
            }
            string password = string.Empty;
            user.Mobile = "1000";
            if (user.Password.IsNotNullOrEmpty())
            {
                password = $"password={user.Password}&";
            }
            var result = client.changeAttrs(userAtDomain, $"{password}org_unit_id={user.DepartmentId}&true_name={user.Name}&nick_name={user.Name}&mobile_number={user.Mobile}");
            if (result.code == 0)
            {
                return true;
            }
            throw new Exception(result.message);
        }

        /// <summary>
        /// 删除用户
        /// </summary>
        /// <param name="userAtDomain"></param>
        /// <returns></returns>
        public bool DeleteUser(string userAtDomain)
        {
            var result = client.deleteUser(userAtDomain);
            if (result.code == 0)
            {
                return true;
            }
            Logger.Info("删除邮箱账号:{0}时失败：{1}".FormatTo(userAtDomain, result.message));
            return false;
        }

        /// <summary>
        /// 新增部门
        /// </summary>
        /// <param name="dept"></param>
        /// <returns></returns>
        public bool CreateDept(MailDepartment dept)
        {
            string parentId = dept.ParentId;
            if (parentId == "1")
            {
                parentId = "";
            }
            var result = client.addUnit("a", dept.DepartmentId, $"parent_org_unit_id={parentId}&org_unit_name={dept.Name}");
            if (result.code == 0)
            {
                return true;
            }
            throw new Exception(result.message);
        }

        /// <summary>
        /// 更新部门
        /// </summary>
        /// <param name="dept"></param>
        /// <returns></returns>
        public bool UpdateDept(MailDepartment dept)
        {
            string parentId = dept.ParentId;
            if (parentId == "1")
            {
                parentId = "";
            }
            var result = client.setUnitAttrs("a", dept.DepartmentId, $"parent_org_unit_id={parentId}&org_unit_name={dept.Name}");
            if (result.code == 0)
            {
                return true;
            }
            throw new Exception(result.message);
        }

        /// <summary>
        /// 删除部门
        /// </summary>
        /// <param name="deptId">部门ID</param>
        /// <returns></returns>
        public bool DeleteDept(string deptId)
        {
            var result = client.delUnit("a", deptId);
            if (result.code == 0)
            {
                return true;
            }
            Logger.Info("删除邮箱部门:{0}时失败：{1}".FormatTo(deptId, result.message));
            return false;
        }

        /// <summary>
        /// 同步用户信息到邮箱
        /// </summary>
        public void SyncUserToMail()
        {
            var users = DBContext<IUser>.Instance.Where(o => o.Mail.IsNullOrEmpty());
            foreach (var item in users)
            {
                item.LoginId = GetValidUserId(item.LoginId);
                var result = client.createUser("1", "a", item.LoginId, $"org_unit_id={item.Department.Code}&user_status=0&domain_name=3vjia.com&cos_id=1&true_name={item.Name}&nick_name={item.Name}&mobile_number={item.MobilePhone}");
                if (result.code == 0)
                {
                    item.Mail = item.LoginId + Mail_Domain;
                    Dao.Get().Update(item);
                    Console.WriteLine("成功添加用户：{0}", item.Name);
                }
                else
                {
                    Console.WriteLine(result.message);
                }
            }
        }

        /// <summary>
        /// 获取邮箱登陆sessionId
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        public string UserLogin(string userId)
        {
            if (userId.IsNullOrEmpty()) return string.Empty;
            var result = client.userLogin(userId);
            if (result.code == 0)
            {
                return result.result;
            }
            throw new Exception(result.message);
        }

        /// <summary>
        /// 获取收件箱未读邮件数
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        public string GetUnReadMail(string userId)
        {
            if (userId.IsNullOrEmpty()) return "-";
            try
            {
                var result = client.getAttrs(userId, "mbox.folder.1.newmsgcnt");
                if (result.code == 0)
                {
                    return result.result.Split('=')[1];
                }
            }
            catch { }
            return "-";
        }
    }
}