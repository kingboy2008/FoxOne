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
    /// 组织架构服务接口
    /// </summary>
    [Authorize]
    public class UCController : ApiController
    {
        /// <summary>
        /// 获取所有用户
        /// </summary>
        /// <returns></returns>
        public IEnumerable<UCUser> GetAllUser()
        {
            return DBContext<IUser>.Instance.Where(o => true).Select(o => ConvertToUCUser(o));
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
                Role = o.Roles.Select(r => new UCRole() { DepartmentId = r.DepartmentId, RoleId = r.RoleType.Code, Name = r.RoleType.Name }).ToList(),
                DepartmentLevelCode = o.Department.WBS,
                DepartmentName = o.Department.Name,
                DepartmentId = o.DepartmentId,
                DDId = o.Code,
                DepartmentDDId = o.Department.Code.ConvertTo<int>()
            };
        }

        private UCDepartment ConvertToUCDepartment(IDepartment o)
        {
            return new UCDepartment()
            {
                DepartmentId = o.Id,
                DepartmentDDId = o.Code.ConvertTo<int>(),
                LevelCode = o.WBS,
                Name = o.Name,
                ParentId = o.ParentId,
                ParentDDId = o.Parent.Code.ConvertTo<int>()
            };
        }

        /// <summary>
        /// 根据ID获取特定用户
        /// </summary>
        /// <param name="id">用户ID</param>
        /// <returns></returns>
        public UCUser GetUser(string id)
        {
            if (id.IsNullOrEmpty())
            {
                throw new ArgumentNullException("id");
            }
            var user = DBContext<IUser>.Instance.FirstOrDefault(o => o.Id.Equals(id, StringComparison.OrdinalIgnoreCase) || o.Code.Equals(id, StringComparison.OrdinalIgnoreCase));
            if (user == null)
            {
                throw new FoxOneException("User_Not_Found");
            }
            return ConvertToUCUser(user);
        }

        /// <summary>
        /// 根据部门ID获取该部门的所有用户（不递归）
        /// </summary>
        /// <param name="id">部门ID</param>
        /// <returns></returns>
        public IEnumerable<UCUser> GetUserByDepartmentId(string id)
        {
            var department = DBContext<IDepartment>.Instance.FirstOrDefault(o => o.Id.Equals(id, StringComparison.OrdinalIgnoreCase) || o.Code.Equals(id, StringComparison.OrdinalIgnoreCase));
            if (department == null)
            {
                throw new FoxOneException("Department_Not_Found");
            }
            return department.Member.Select(o => ConvertToUCUser(o));
        }

        /// <summary>
        /// 获取所有部门
        /// </summary>
        /// <returns></returns>
        public IEnumerable<UCDepartment> GetAllDepartment()
        {
            return DBContext<IDepartment>.Instance.Where(o => o.ParentId.IsNotNullOrEmpty()).Select(o => ConvertToUCDepartment(o));
        }

        /// <summary>
        /// 根据部门ID获取特定部门
        /// </summary>
        /// <param name="id">部门ID</param>
        /// <returns></returns>
        public UCDepartment GetDepartment(string id)
        {
            var o = DBContext<IDepartment>.Instance.FirstOrDefault(d => d.Id.Equals(id, StringComparison.OrdinalIgnoreCase) || d.Code.Equals(id, StringComparison.OrdinalIgnoreCase));
            return ConvertToUCDepartment(o);
        }

        /// <summary>
        /// 根据系统编号及用户ID获取该用户在指定系统中的所有权限
        /// </summary>
        /// <param name="id">用户ID</param>
        /// <param name="systemId">系统编号</param>
        /// <returns></returns>
        public IEnumerable<UCPermission> GetAllUserPermission(string id, string systemId)
        {
            var user = DBContext<IUser>.Instance.FirstOrDefault(o => o.Code.Equals(id, StringComparison.OrdinalIgnoreCase));
            if (user == null)
            {
                throw new FoxOneException("User_Not_Exist");
            }
            var systemPermission = DBContext<IPermission>.Instance.FirstOrDefault(o => o.Type == PermissionType.System && o.Code.Equals(systemId, StringComparison.OrdinalIgnoreCase));
            if (systemPermission == null)
            {
                throw new FoxOneException("System_Not_Exist");
            }
            var allUserPermission = Sec.Provider.GetAllUserPermission(user);
            var result = new List<UCPermission>();
            foreach (var module in systemPermission.Childrens)
            {
                foreach (var page in module.Childrens)
                {
                    if (page.Type == PermissionType.Page)
                    {
                        if (allUserPermission.Any(o => o.Id.Equals(page.Id, StringComparison.OrdinalIgnoreCase)))
                        {
                            var temp = new UCPermission()
                            {
                                Id = page.Code,
                                Url = page.Url,
                                Name = page.Name,
                                ControlIds = string.Empty
                            };
                            var ctrls = page.Childrens.Where(o => allUserPermission.Any(j => j.Id == o.Id));
                            if (!ctrls.IsNullOrEmpty())
                            {
                                temp.ControlIds = string.Join(",", ctrls.Select(o => o.Url).ToArray());
                            }
                            result.Add(temp);
                        }
                    }
                }
            }
            return result;
        }

        /// <summary>
        /// 根据authCode获取当前用户信息
        /// </summary>
        /// <param name="id">authCode</param>
        /// <returns></returns>
        public UCUser GetCurrentUser(string id)
        {
            var ddUser = DDHelper.GetUserInfo(id);
            return GetUser(ddUser);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public SignPackage FetchSignPackage(string id)
        {
            return DDHelper.FetchSignPackage(id);
        }
    }

    /// <summary>
    /// 用户信息
    /// </summary>
    public class UCUser
    {
        /// <summary>
        /// 用户ID
        /// </summary>
        public string UserId { get; set; }

        /// <summary>
        /// 登陆账号
        /// </summary>
        public string LoginId { get; set; }

        /// <summary>
        /// 用户在钉钉中的ID
        /// </summary>
        public string DDId { get; set; }

        /// <summary>
        /// 用户姓名
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 用户手机号（唯一）
        /// </summary>
        public string Mobile { get; set; }

        /// <summary>
        /// 邮箱
        /// </summary>
        public string Mail { get; set; }

        /// <summary>
        /// 用户所属部门ID
        /// </summary>
        public string DepartmentId { get; set; }

        /// <summary>
        /// 用户所属部门在钉钉中的ID
        /// </summary>
        public int DepartmentDDId { get; set; }

        /// <summary>
        /// 用户所属部门名称
        /// </summary>
        public string DepartmentName { get; set; }

        /// <summary>
        /// 用户所属部门层级编码
        /// </summary>
        public string DepartmentLevelCode { get; set; }

        /// <summary>
        /// 用户角色
        /// </summary>
        public IList<UCRole> Role { get; set; }

    }

    /// <summary>
    /// 角色信息
    /// </summary>
    public class UCRole
    {
        /// <summary>
        /// 角色ID
        /// </summary>
        public string RoleId { get; set; }

        /// <summary>
        /// 角色名称
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 角色所属部门
        /// </summary>
        public string DepartmentId { get; set; }
    }

    /// <summary>
    /// 部门信息
    /// </summary>
    public class UCDepartment
    {
        /// <summary>
        /// 部门ID
        /// </summary>
        public string DepartmentId { get; set; }

        /// <summary>
        /// 部门在钉钉中的ID
        /// </summary>
        public int DepartmentDDId { get; set; }

        /// <summary>
        /// 部门名称
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 部门层级编码
        /// </summary>
        public string LevelCode { get; set; }

        /// <summary>
        /// 父级部门ID
        /// </summary>
        public string ParentId { get; set; }

        /// <summary>
        /// 父级部门在钉钉中的ID
        /// </summary>
        public int ParentDDId { get; set; }
    }

    /// <summary>
    /// 权限信息
    /// </summary>
    public class UCPermission
    {
        /// <summary>
        /// 权限ID
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// 权限名称
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 页面URL
        /// </summary>
        public string Url { get; set; }

        /// <summary>
        /// 拥有权限的控件ID
        /// </summary>
        public string ControlIds { get; set; }
    }
}
