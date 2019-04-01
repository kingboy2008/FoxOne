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
        /// 用户状态：enabled在职，disabled离职
        /// </summary>
        public string Status { get; set; }

        /// <summary>
        /// 头像
        /// </summary>
        public string Avatar { get; set; }

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
        /// 部门状态：enabled表示可用，disabled表示不可用
        /// </summary>
        public string Status { get; set; }

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

    /// <summary>
    /// 请求参数
    /// </summary>
    public class IdParameter
    {
        /// <summary>
        /// 多个ID的集合，用逗号隔开
        /// </summary>
        public string Ids { get; set; }
    }
}
