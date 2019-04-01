using FoxOne.Core;
using System.Linq; 
namespace FoxOne.Business.Security
{
    public sealed class Sec
    {
        private static ISecurityProvider _provider;

        /// <summary>
        /// 当前登录用户
        /// </summary>
        public static IUser User
        {
            get { return Provider.GetCurrentUser(); }
        }

        public static ISecurityProvider Provider
        {
            get 
            {
                return _provider ?? (_provider = new SecurityProvider());
            }
        }

        /// <summary>
        /// 当前登录用户是否为超级管理员
        /// </summary>
        public static bool IsSuperAdmin
        {
            get
            {
                return User.Roles.Count(o => o.RoleType.Name.Equals(SysConfig.SuperAdminRoleName)) > 0;
            }
        }
    }
}