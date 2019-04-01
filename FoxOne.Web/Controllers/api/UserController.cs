using FoxOne.Business;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http;
using FoxOne.Core;
using FoxOne.Business.Security;
using FoxOne.Business.DDSDK;
using FoxOne.Business.DDSDK.Entity;
using System.Runtime.Serialization;

namespace FoxOne.Web.Controllers
{
    /// <summary>
    /// 用户信息接口
    /// </summary>
    [CustomApiAuthorize]
    public class UserController : ApiController
    {
        /// <summary>
        /// 获取全部用户信息
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public IEnumerable<UCUser> List()
        {
            return DBContext<IUser>.Instance.Select(o => ConvertToUCUser(o));
        }

        /// <summary>
        /// 获取单个用户信息
        /// </summary>
        /// <param name="id">用户ID</param>
        /// <returns></returns>
        [HttpGet]
        public UCUser Get(string id)
        {
            if (id.IsNullOrEmpty())
            {
                throw new FoxOneException("Parameter_Not_Null", "id");
            }
            var user = DBContext<IUser>.Instance.FirstOrDefault(o => o.Id.Equals(id, StringComparison.OrdinalIgnoreCase) || o.Code.Equals(id, StringComparison.OrdinalIgnoreCase));
            if (user == null)
            {
                throw new FoxOneException("User_Not_Found", id);
            }
            return ConvertToUCUser(user);
        }

        /// <summary>
        /// 根据姓名在指定部门ID下模糊搜索用户
        /// </summary>
        /// <param name="name">用户姓名</param>
        /// <param name="departmentId">部门ID，为空则搜索全部</param>
        /// <returns></returns>
        [HttpGet]
        public IEnumerable<UCUser> ListByName(string name, string departmentId = default(string))
        {
            if (name.IsNullOrEmpty())
            {
                throw new FoxOneException("Parameter_Not_Null", "name");
            }
            if (departmentId.IsNullOrEmpty())
            {
                return DBContext<IUser>.Instance.Where(o => o.Name.IndexOf(name) >= 0).Select(o => ConvertToUCUser(o));
            }
            else
            {
                return ListByDepartmentId(departmentId, true).Where(o => o.Name.IndexOf(name) >= 0);
            }
        }

        /// <summary>
        /// 根据多个用户ID获取多个用户信息
        /// </summary>
        /// <param name="param">多个用户ID，用逗号隔开</param>
        /// <returns></returns>
        [HttpPost]
        public IEnumerable<UCUser> ListByMultipleIds(IdParameter param)
        {
            if (param == null || param.Ids.IsNullOrEmpty())
            {
                throw new FoxOneException("Parameter_Not_Null", "ids");
            }
            string[] idArr = param.Ids.Split(',');
            return DBContext<IUser>.Instance.Where(o => idArr.Contains(o.Id, StringComparer.OrdinalIgnoreCase)).Select(o => ConvertToUCUser(o));

        }

        /// <summary>
        /// 根据部门ID获取部门内所有用户
        /// </summary>
        /// <param name="departmentId">部门ID</param>
        /// <param name="recursion">是否递归</param>
        /// <returns></returns>
        [HttpGet]
        public IEnumerable<UCUser> ListByDepartmentId(string departmentId, bool recursion = false)
        {
            if (departmentId.IsNullOrEmpty())
            {
                throw new FoxOneException("Parameter_Not_Null", "departmentId");
            }
            var department = DBContext<IDepartment>.Instance.FirstOrDefault(o => o.Id.Equals(departmentId, StringComparison.OrdinalIgnoreCase) || o.Code.Equals(departmentId, StringComparison.OrdinalIgnoreCase));
            if (department == null)
            {
                throw new FoxOneException("Department_Not_Found", departmentId);
            }
            if (recursion)
            {
                var depts = DBContext<IDepartment>.Instance.Where(o => o.WBS.StartsWith(department.WBS));
                var result = new List<UCUser>();
                foreach (var item in depts)
                {
                    result.AddRange(item.Member.Select(o => ConvertToUCUser(o)));
                }
                return result;
            }
            else
            {
                return department.Member.Select(o => ConvertToUCUser(o));
            }
        }

        /// <summary>
        /// 根据上级用户ID获取所有下属
        /// </summary>
        /// <param name="supervisorId">上级用户ID</param>
        /// <returns></returns>
        [HttpGet]
        public IEnumerable<UCUser> ListBySupervisorId(string supervisorId)
        {
            if (supervisorId.IsNullOrEmpty())
            {
                throw new FoxOneException("Parameter_Not_Null", "supervisorId");
            }
            string departmentId = string.Empty;
            var user = Get(supervisorId);
            if (user.Role.Any(o => o.RoleId.Equals("Manager", StringComparison.OrdinalIgnoreCase) || o.RoleId.Equals("ChiefInspector", StringComparison.OrdinalIgnoreCase)))
            {
                departmentId = user.DepartmentId;
                return ListByDepartmentId(departmentId);
            }
            else
            {
                throw new FoxOneException("User_Is_Not_Supervisor");
            }
        }


        /// <summary>
        /// 根据authCode获取当前用户信息
        /// </summary>
        /// <param name="authCode">authCode</param>
        /// <returns></returns>
        [HttpGet]
        public UCUser GetCurrentUser(string authCode)
        {
            var ddUser = DDHelper.GetUserInfo(authCode);
            if (ddUser.IsNullOrEmpty())
            {
                throw new FoxOneException("Invalid_AuthCode");
            }
            return Get(ddUser);
        }

        private UCUser ConvertToUCUser(IUser o)
        {
            return new UCUser()
            {
                LoginId = o.LoginId,
                Mail = o.Mail,
                UserId = o.Id,
                Name = o.Name,
                Mobile = o.MobilePhone,
                Role = o.Roles.IsNullOrEmpty() ? new List<UCRole>() : o.Roles.Select(r => new UCRole() { DepartmentId = r.DepartmentId, RoleId = r.RoleType.Code, Name = r.RoleType.Name }).ToList(),
                DepartmentLevelCode = o.Department.WBS,
                DepartmentName = o.Department.Name,
                DepartmentId = o.DepartmentId,
                DDId = o.Code,
                DepartmentDDId = o.Department.Code.ConvertTo<int>(),
                Status = o.Status,
                Avatar = o.Avatar
            };
        }
    }
}