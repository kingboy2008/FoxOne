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

namespace FoxOne.Web.Controllers.api
{
    /// <summary>
    /// 权限信息接口
    /// </summary>
    [CustomApiAuthorize]
    public class PermissionController : ApiController
    {
        /// <summary>
        /// 根据系统编号及用户ID获取该用户在指定系统中的所有权限
        /// </summary>
        /// <param name="id">用户ID</param>
        /// <param name="systemId">系统编号</param>
        /// <returns></returns>
        [HttpGet]
        public IEnumerable<UCPermission> List(string id, string systemId)
        {
            if (id.IsNullOrEmpty())
            {
                throw new FoxOneException("Parameter_Not_Null", "id");
            }
            if (systemId.IsNullOrEmpty())
            {
                throw new FoxOneException("Parameter_Not_Null", "systemId");
            }
            var user = DBContext<IUser>.Instance.FirstOrDefault(o => o.Id.Equals(id, StringComparison.OrdinalIgnoreCase) || o.Code.Equals(id, StringComparison.OrdinalIgnoreCase));
            if (user == null)
            {
                throw new FoxOneException("User_Not_Found", id);
            }
            var systemPermission = DBContext<IPermission>.Instance.FirstOrDefault(o => o.Type == PermissionType.System && o.Code.Equals(systemId, StringComparison.OrdinalIgnoreCase));
            if (systemPermission == null)
            {
                throw new FoxOneException("System_Not_Found", systemId);
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
        /// 判断用户是否有特定URL的访问权限
        /// </summary>
        /// <param name="id">用户ID</param>
        /// <param name="systemId">系统编号</param>
        /// <param name="url">URL</param>
        /// <returns></returns>
        [HttpGet]
        public bool HasPermission(string id, string systemId, string url)
        {
            if (id.IsNullOrEmpty())
            {
                throw new FoxOneException("Parameter_Not_Null", "id");
            }
            if (systemId.IsNullOrEmpty())
            {
                throw new FoxOneException("Parameter_Not_Null", "systemId");
            }
            if (url.IsNullOrEmpty())
            {
                throw new FoxOneException("Parameter_Not_Null", "url");
            }
            url = HttpUtility.UrlDecode(url);
            return List(id, systemId).Any(o => url.StartsWith(o.Url) || o.Id.Equals(url, StringComparison.OrdinalIgnoreCase));
        }
    }
}