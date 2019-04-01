using FoxOne.Workflow.Kernel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http.ModelBinding;
using System.Web.Mvc;
using FoxOne.Core;
namespace FoxOne.Web
{
    /// <summary>
    /// 流程运行参数
    /// </summary>
    //[System.Web.Mvc.ModelBinder(typeof(WorkflowRunParameterBinder))]
    public class WorkflowRunParameter : WorkflowParameter
    {
        /// <summary>
        /// 用户选择结果
        /// </summary>
        public IList<WorkflowRunChoice> UserChoice { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public IList<IWorkflowChoice> RunContext()
        {

            var result = new List<IWorkflowChoice>();
            if (!UserChoice.IsNullOrEmpty())
            {
                UserChoice.ForEach(o =>
                {
                    var item = result.FirstOrDefault(r => r.Choice.Equals(o.StepName, StringComparison.OrdinalIgnoreCase));
                    if (item == null)
                    {
                        item = ObjectHelper.GetObject<IWorkflowChoice>();
                        item.Choice = o.StepName;
                        result.Add(item);
                    }
                    if (item.Participant == null)
                    {
                        item.Participant = new List<IUser>();
                    }
                    if (o.Id.IsNotNullOrEmpty())
                    {
                        var user = DBContext<IUser>.Instance.FirstOrDefault(u => u.Id.Equals(o.Id, StringComparison.OrdinalIgnoreCase));
                        user.DepartmentId = o.DepartmentId;
                        item.Participant.Add(user);
                    }
                });
            }
            return result;

        }
    }
    /// <summary>
    /// 用户选择内容
    /// </summary>
    public class WorkflowRunChoice
    {
        /// <summary>
        /// 用户ID
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// 部门ID
        /// </summary>
        public string DepartmentId { get; set; }

        /// <summary>
        /// 步骤名称
        /// </summary>
        public string StepName { get; set; }
    }

    /// <summary>
    /// 发起流程所需参数
    /// </summary>
    public class WorkflowStartParameter
    {
        /// <summary>
        /// 流程应用ID
        /// </summary>
        public string AppCode { get; set; }

        /// <summary>
        /// 流程实例名称
        /// </summary>
        public string InstanceName { get; set; }

        /// <summary>
        /// 发起人ID
        /// </summary>
        public string CreatorId { get; set; }

        /// <summary>
        /// 表单主键
        /// </summary>
        public string DataLocator { get; set; }

        /// <summary>
        /// 缓急：0普通/1紧急/2特急
        /// </summary>
        public int ImportLevel { get; set; }

        /// <summary>
        /// 密级：0普通/2机密
        /// </summary>
        public int SecurityLevel { get; set; }
    }

    /// <summary>
    /// 获取下一步所需参数
    /// </summary>
    public class WorkflowNextStepParameter
    {
        /// <summary>
        /// 流程实例号
        /// </summary>
        public string InstanceId { get; set; }

        /// <summary>
        /// 当前操作用户ID
        /// </summary>
        public string CurrentUserId { get; set; }

        /// <summary>
        /// 当前工作项ID
        /// </summary>
        public int ItemId { get; set; }

        /// <summary>
        /// 审批意见
        /// </summary>
        public string OpinionContent { get; set; }

        /// <summary>
        /// 意见类型，暂时用不到
        /// </summary>
        public int OpinionArea { get; set; }
    }

    /// <summary>
    /// 运行流程所需参数
    /// </summary>
    public class WorkflowParameter : WorkflowNextStepParameter
    {
        /// <summary>
        /// 运行命令：run运行/rollback撤回/pushback退回上一步/backtoroot退回拟稿人/forceend终止审批/delete删除流程
        /// </summary>
        public string Command { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
    public class WorkflowRunParameterBinder : System.Web.Mvc.IModelBinder
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="controllerContext"></param>
        /// <param name="bindingContext"></param>
        /// <returns></returns>
        public object BindModel(ControllerContext controllerContext, System.Web.Mvc.ModelBindingContext bindingContext)
        {
            return controllerContext.HttpContext.Request.Form.ToEntity<WorkflowRunParameter>();
        }
    }
}