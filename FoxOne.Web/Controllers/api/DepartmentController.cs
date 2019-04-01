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
using System.Web.Http.Controllers;

namespace FoxOne.Web.Controllers
{
    /// <summary>
    /// 部门信息接口
    /// </summary>
    [CustomApiAuthorize]
    public class DepartmentController : ApiController
    {
        /// <summary>
        /// 根据部门ID获取特定部门
        /// </summary>
        /// <param name="id">部门ID</param>
        /// <returns></returns>
        [HttpGet]
        public UCDepartment Get(string id)
        {
            if (id.IsNullOrEmpty())
            {
                throw new FoxOneException("Parameter_Not_Null", "id");
            }
            var o = DBContext<IDepartment>.Instance.FirstOrDefault(d => d.Id.Equals(id, StringComparison.OrdinalIgnoreCase) || d.Code.Equals(id, StringComparison.OrdinalIgnoreCase));
            if (o == null)
            {
                throw new FoxOneException("Department_Not_Found", id);
            }
            return ConvertToUCDepartment(o);
        }

        /// <summary>
        /// 获取所有部门
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public IEnumerable<UCDepartment> List()
        {
            return DBContext<IDepartment>.Instance.Select(o => ConvertToUCDepartment(o));
        }

        /// <summary>
        /// 根据多个部门ID返回多个部门信息
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        [HttpPost]
        public IEnumerable<UCDepartment> ListByMultipleIds(IdParameter param)
        {
            if (param == null || param.Ids.IsNullOrEmpty())
            {
                throw new FoxOneException("Parameter_Not_Null", "ids");
            }
            string[] idArry = param.Ids.Split(',');
            return DBContext<IDepartment>.Instance.Where(o => idArry.Contains(o.Id, StringComparer.OrdinalIgnoreCase)).Select(o => ConvertToUCDepartment(o));
        }

        /// <summary>
        /// 获取指定部门的子级
        /// </summary>
        /// <param name="id">部门ID</param>
        /// <param name="recursion">是否递归</param>
        /// <returns></returns>
        [HttpGet]
        public IEnumerable<UCDepartment> Children(string id, bool recursion = false)
        {
            if (id.IsNullOrEmpty())
            {
                throw new FoxOneException("Parameter_Not_Null", "id");
            }
            var dept = DBContext<IDepartment>.Instance.FirstOrDefault(o => o.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
            if (dept == null)
            {
                throw new FoxOneException("Department_Not_Found", id);
            }
            if (recursion)
            {
                return DBContext<IDepartment>.Instance.Where(o => o.WBS.StartsWith(dept.WBS)).Select(o => ConvertToUCDepartment(o));
            }
            else
            {
                return dept.Childrens.Select(o => ConvertToUCDepartment(o));
            }
        }

        private UCDepartment ConvertToUCDepartment(IDepartment o)
        {
            return new UCDepartment()
            {
                DepartmentId = o.Id,
                DepartmentDDId = o.Code.ConvertTo<int>(),
                LevelCode = o.WBS,
                Name = o.Name,
                ParentId = o.ParentId.IsNullOrEmpty() ? o.Id : o.ParentId,
                ParentDDId = o.ParentId.IsNullOrEmpty() ? 1 : o.Parent.Code.ConvertTo<int>(),
                Status = o.Status
            };
        }
    }
}