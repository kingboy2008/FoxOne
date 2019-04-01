using FoxOne.Business;
using FoxOne.Core;
using FoxOne.Workflow.Kernel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Web;

namespace FoxOne.Web    
{
    /// <summary>
    /// 工作流附件转换器
    /// </summary>
    [DisplayName("工作流附件转换器")]
    public class WorkflowAttachmentFilter : ColumnConverterBase
    {
        private const string DATALOCATOR = "DataLocator";

        public WorkflowAttachmentFilter()
        {
            InstanceIdFieldName = "InstanceId";
        }

        /// <summary>
        /// 流程实例字段名
        /// </summary>
        [DisplayName("流程实例字段名")]
        public string InstanceIdFieldName { get; set; }

        public override object Converter(object value)
        {
            //return base.Converter(value);
            var instanceId = RowData[InstanceIdFieldName].ToString();
            string key = string.Empty;
            if (RowData.ContainsKey(DATALOCATOR))
            {
                key = RowData[DATALOCATOR].ToString();
            }
            else
            {
                key = DBContext<IWorkflowInstance>.Instance.FirstOrDefault(c => c.Id.Equals(instanceId)).DataLocator;
            }
            int count = DBContext<AttachmentEntity>.Instance.Count(c => c.RelateId.Equals(key));
            if (count > 0)
            {
                return $"{value}<img style='margin-left:2px;' src='/images/icons/attachment.png'>";
            }
            return value;
        }
    }
}