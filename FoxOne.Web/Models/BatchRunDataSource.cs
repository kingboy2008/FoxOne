using FoxOne.Business;
using FoxOne.Business.Security;
using FoxOne.Core;
using FoxOne.Workflow.Business;
using FoxOne.Workflow.Kernel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Web;

namespace FoxOne.Web
{
    [DisplayName("批量审批数据源")]
    public class BatchRunDataSource : CRUDDataSource
    {
        public string ApplicationId { get; set; }

        public override IEnumerable<IDictionary<string, object>> GetList()
        {
            var ids = WorkflowHelper.GetToDoList(Sec.User.Id).Where(o => o.ApplicationId == ApplicationId).Select(o => o.DataLocator).ToArray();
            if (ids.Length == 0)
            {
                return null;
            }
            else
            {
                Parameter["Ins"] = ids;
                return base.GetList();
            }
        }

        public override IEnumerable<IDictionary<string, object>> GetList(int pageIndex, int pageSize, out int recordCount)
        {
            recordCount = 0;
            var ids = WorkflowHelper.GetToDoList(Sec.User.Id).Where(o => o.ApplicationId == ApplicationId).Select(o => o.DataLocator).ToArray();
            if(ids.Length==0)
            {
                return null;
            }
            else
            {
                Parameter["Ins"] = ids;
                return base.GetList(pageIndex, pageSize, out recordCount);
            }
        }
    }

    [DisplayName("待办分组数据源")]
    public class WorkflowApplicationGroupByDataSource : ListDataSourceBase
    {
        protected override IEnumerable<IDictionary<string, object>> GetListInner()
        {
            var todoListGp = WorkflowHelper.GetToDoList(Sec.User.Id).GroupBy(o => o.ApplicationId);
            foreach (var item in todoListGp)
            {
                var app = DBContext<IWorkflowApplication>.Instance.FirstOrDefault(o => o.Id == item.Key);
                yield return new Dictionary<string, object>() { { "Id", item.Key }, { "Name", app.Name }, { "Count", item.Count() }, { "Url", app.DocUrl }, { "Link", app.DocUrl.IsNullOrEmpty() ? "未开启批量处理" : "点击可批量处理" }, { "Icon", app.Icon } };
            }
        }
    }
}