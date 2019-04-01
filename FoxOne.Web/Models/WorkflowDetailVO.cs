using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using FoxOne.Business;
using FoxOne.Controls;
namespace FoxOne.Web
{
    /// <summary>
    /// 流程实例详情视图对象
    /// </summary>
    public class WorkflowDetailVO
    {
        /// <summary>
        /// 标题
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// 是否允许删除实例
        /// </summary>
        public bool CanDeleteInstance { get; set; }

        /// <summary>
        /// 实例号
        /// </summary>
        public string InstanceId { get; set; }

        /// <summary>
        /// 工作项号
        /// </summary>
        public int ItemId { get; set; }

        /// <summary>
        /// 是否处于模拟状态
        /// </summary>
        public string IsSimulate { get; set; }

        /// <summary>
        /// 能否上传附件
        /// </summary>
        public bool CanUploadFile { get; set; }


        /// <summary>
        /// 能否编辑主表单
        /// </summary>
        public bool CanEditForm { get; set; }

        /// <summary>
        /// 当前流程定义ID号
        /// </summary>
        public string DefinitionId { get; set; }

        /// <summary>
        /// 附件信息
        /// </summary>
        public IEnumerable<AttachmentInfoVO> Attachment { get; set; }

        /// <summary>
        /// 是否显示附件
        /// </summary>
        public bool ShowAttachment { get; set; }

        /// <summary>
        /// 审批信息
        /// </summary>
        public IEnumerable<WorkItemVO> WorkItem { get; set; }

        /// <summary>
        /// 知会信息
        /// </summary>
        public IEnumerable<WorkItemVO> NoticeItem { get; set; }

        /// <summary>
        /// 是否显示审批信息
        /// </summary>
        public bool ShowWorkItem { get; set; }

        /// <summary>
        /// 是否显示意见填写区域
        /// </summary>
        public bool ShowOpinionInput { get; set; }

        /// <summary>
        /// 主表单
        /// </summary>
        public Form Form { get; set; }

        /// <summary>
        /// 关联页面
        /// </summary>
        public IEnumerable<Page> RelatePage { get; set; }

        /// <summary>
        /// 工具栏按钮
        /// </summary>
        public IList<WorkflowButtonType> ToolbarButtons { get; set; }

    }
}