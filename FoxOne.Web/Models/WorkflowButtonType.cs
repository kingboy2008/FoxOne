using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Web;

namespace FoxOne.Web
{
    /// <summary>
    /// 工作流数据源类型
    /// </summary>
    public enum WorkflowDataSourceType
    {
        /// <summary>
        /// 待办
        /// </summary>
        [Description("待办")]
        ToDo,

        /// <summary>
        /// 已办
        /// </summary>
        [Description("已办")]
        Done,

        /// <summary>
        /// 知会
        /// </summary>
        [Description("知会")]
        Read,

        /// <summary>
        /// 流程定义
        /// </summary>
        [Description("流程定义")]
        Definition,

        /// <summary>
        /// 流程应用
        /// </summary>
        [Description("流程应用")]
        Application,

        /// <summary>
        /// 流程实例
        /// </summary>
        [Description("流程实例")]
        Instance,
    }

    /// <summary>
    /// 工作流按钮类型
    /// </summary>
    public enum WorkflowButtonType
    {
        /// <summary>
        /// 保存
        /// </summary>
        [Description("保存")]
        Save = 1,

        /// <summary>
        /// 发送
        /// </summary>
        [Description("发送")]
        Send = 2,

        /// <summary>
        /// 删除
        /// </summary>
        [Description("删除")]
        Delete = 3,

        /// <summary>
        /// 撤回
        /// </summary>
        [Description("撤回")]
        Rollback = 4,

        /// <summary>
        /// 回退
        /// </summary>
        [Description("回退")]
        Pushback = 5,

        /// <summary>
        /// 退回拟稿
        /// </summary>
        [Description("退回拟稿")]
        BackToRoot = 6,

        /// <summary>
        /// 终止审批
        /// </summary>
        [Description("终止审批")]
        ForceEnd = 7,

        /// <summary>
        /// 传阅
        /// </summary>
        [Description("传阅")]
        SendOtherToRead = 8
    }
}