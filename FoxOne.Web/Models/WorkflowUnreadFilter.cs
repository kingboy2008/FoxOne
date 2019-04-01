using FoxOne.Business;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Web;
using FoxOne.Core;
namespace FoxOne.Web
{
    /// <summary>
    /// 工作流未读过滤器
    /// </summary>
    [DisplayName("工作流未读过滤器")]
    public class WorkflowUnreadFilter : RequestParameterDataFilter
    {
        /// <summary>
        /// 排除的字段
        /// </summary>
        [DisplayName("排除的字段")]
        public string ExceptFields { get; set; }

        public override bool Filter(IDictionary<string, object> data)
        {
            var result = base.Filter(data);
            var arr = ExceptFields.Split(new char[] { ',', ';', '|' }, StringSplitOptions.RemoveEmptyEntries).ToList();
            if (!data.ContainsKey("FinishTime") || data["FinishTime"] == null || data["FinishTime"].ToString().IsNullOrEmpty())
            {
                var keys = data.Keys.ToList();
                foreach (var key in keys)
                {
                    if (!arr.Contains(key))
                    {
                        data[key] = $"<b>{data[key]}</b>";
                    }
                }
            }
            return result;
        }
    }
}