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
using FoxOne.Workflow.Business;
using FoxOne.Controls;
using System.Transactions;

namespace FoxOne.Web.Controllers.api
{
    /// <summary>
    /// 工作流相关api
    /// </summary>
    [CustomApiAuthorize]
    public class WorkflowController : ApiController
    {
        /// <summary>
        /// 发起新的流程
        /// </summary>
        /// <param name="entity">发起流程所需参数</param>
        /// <returns>流程实例号</returns>
        [HttpPost]
        public string Start(WorkflowStartParameter entity)
        {
            var helper = new WorkflowHelper(entity.CreatorId);
            var application = WorkflowHelper.GetApplication(entity.AppCode);
            if (application == null)
            {
                throw new FoxOneException("不存在流程应用编号为：{0}的流程应用", entity.AppCode);
            }
            helper.StartWorkflow(entity.AppCode, entity.InstanceName, entity.DataLocator, entity.ImportLevel, entity.SecurityLevel);
            return helper.FlowInstance.Id;
        }

        /// <summary>
        /// 运行流程
        /// </summary>
        /// <param name="runParameter">流程运行所需参数</param>
        /// <returns></returns>
        [HttpPost]
        public bool Run(WorkflowRunParameter runParameter)
        {
            return ExecCommand("run", runParameter);
        }


        /// <summary>
        /// 撤回流程
        /// </summary>
        /// <param name="runParameter">流程运行所需参数</param>
        /// <returns></returns>
        [HttpPost]
        public bool Rollback(WorkflowNextStepParameter runParameter)
        {
            return ExecCommand("rollback", runParameter);
        }


        /// <summary>
        /// 退回上一步
        /// </summary>
        /// <param name="runParameter">流程运行所需参数</param>
        /// <returns></returns>
        [HttpPost]
        public bool Pushback(WorkflowNextStepParameter runParameter)
        {
            return ExecCommand("pushback", runParameter);
        }


        /// <summary>
        /// 退回拟稿人
        /// </summary>
        /// <param name="runParameter">流程运行所需参数</param>
        /// <returns></returns>
        [HttpPost]
        public bool BackToRoot(WorkflowNextStepParameter runParameter)
        {
            return ExecCommand("backtoroot", runParameter);
        }


        /// <summary>
        /// 终止审批
        /// </summary>
        /// <param name="runParameter">流程运行所需参数</param>
        /// <returns></returns>
        [HttpPost]
        public bool ForceEnd(WorkflowNextStepParameter runParameter)
        {
            return ExecCommand("forceend", runParameter);
        }

        /// <summary>
        /// 根据用户ID获取待办事项
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <returns></returns>
        public IList<ToDoList> GetToDoList(string userId)
        {
            return WorkflowHelper.GetToDoList(userId);
        }


        /// <summary>
        /// 根据用户ID获取已办事项
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <returns></returns>
        public IList<ToDoList> GetDoneList(string userId)
        {
            return WorkflowHelper.GetDoneList(userId);
        }


        /// <summary>
        /// 根据用户ID获取传阅事项
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <returns></returns>
        public IList<ToDoList> GetReadList(string userId)
        {
            return WorkflowHelper.GetReadList(userId);
        }

        /// <summary>
        /// 根据实例Id获取轨迹列表
        /// </summary>
        /// <param name="instanceId">实例Id</param>
        /// <param name="datalocator">主表单Id</param>
        /// <returns></returns>
        public IList<WorkItemVO> GetWorkItemList(string instanceId=default(string),string datalocator=default(string))
        {
            //if (!SysConfig.IsProductEnv)
            //{
            //    System.Threading.Thread.Sleep(10000);
            //}
            if (instanceId.IsNullOrEmpty())
            {
                if (datalocator.IsNullOrEmpty())
                {
                    throw new FoxOneException("instanceId and datalocator are null");
                }
                instanceId = Data.Dao.Get().Query<Workflow.DataAccess.WorkflowInstance>().FirstOrDefault(c => c.DataLocator == datalocator).Id;
            }
            return Data.Dao.Get().Query<Workflow.DataAccess.WorkflowItem>().Where(c => c.InstanceId == instanceId).OrderBy(c=>c.ItemId).ToList().Select(o=> new WorkItemVO()
            {
                ActivityName = o.Alias,
                ItemId = o.ItemId,
                CreatorUserId = o.PartUserId,
                Opinion = o.OpinionContent,
                ShowTime = o.ItemId == 1 ? (o.ReceiveTime.Value.ToString("申请时间：yyyy-MM-dd HH:mm")) : (o.FinishTime.HasValue ? o.FinishTime.Value.ToString("审批时间：yyyy-MM-dd HH:mm") : (o.ReadTime.HasValue ? o.ReadTime.Value.ToString("阅读时间：yyyy-MM-dd HH:mm") : o.ReceiveTime.Value.ToString("送达时间：yyyy-MM-dd HH:mm"))),
                Status = o.Status.ToString(),
                StatusText = o.ItemId == 1 ? "申请" : o.StatusText,
                Creator = o.PartUserName,
            }).ToList();
        }

        private IFormService GetForm(WorkflowHelper helper)
        {
            var formType = helper.FlowInstance.Application.FormType;
            var page = PageBuilder.BuildPage(formType);
            var form = page.Controls.FirstOrDefault(o => o is Form) as Form;
            form.Key = helper.FlowInstance.DataLocator;
            var parameter = form.FormService.Get(form.Key);//默认把表单输入域的所有值都当成是流程参数
            var result = form.FormService;
            if (!parameter.IsNullOrEmpty())
            {
                foreach (var item in parameter)
                {
                    helper.SetParameter(item.Key, item.Value.ToString());
                }
            }

            return result;
        }

        /// <summary>
        /// 获取下一步可选步骤，如果下一步不需要选择，则当前操作会引起流程直接发送到下一步
        /// </summary>
        /// <param name="runParameter"></param>
        /// <returns></returns>
        [HttpPost]
        public IList<NextStep> GetNextStep(WorkflowNextStepParameter runParameter)
        {
            List<NextStep> trans = new List<NextStep>();
            WorkflowHelper helper = new WorkflowHelper(runParameter.CurrentUserId);
            helper.OpenWorkflow(runParameter.InstanceId, runParameter.ItemId);
            var form = GetForm(helper) as IFlowFormService;
            if (form == null || form.CanRunFlow())
            {
                if (!helper.ShowUserSelect())
                {
                    if (!string.IsNullOrEmpty(runParameter.OpinionContent))
                    {
                        helper.SetOpinion(runParameter.OpinionContent, runParameter.OpinionArea);
                    }
                    helper.Run();
                    trans.Add(new NextStep() { StepName = "自动发送", Label = "自动发送" });
                }
                else
                {
                    trans = helper.GetNextStep();
                    if (trans.Count == 1)
                    {
                        var step = trans[0];
                        if (step.Users.Count == 1 || step.NeedUser == false)
                        {
                            if (!string.IsNullOrEmpty(runParameter.OpinionContent))
                            {
                                helper.SetOpinion(runParameter.OpinionContent, runParameter.OpinionArea);
                            }
                            if (step.Users == null || step.Users.Count == 0)
                            {
                                helper.Run(helper.GetUserChoice("NULL_NULL_" + step.StepName));
                            }
                            else
                            {
                                helper.Run(helper.GetUserChoice(step.StepName, step.Users[0]));
                            }
                            trans[0].StepName = "自动发送";
                        }
                    }
                }
                if (trans.Count == 0)
                {
                    throw new FoxOneException("当前步骤无下一步可用迁移，请检查流程图及表单参数的设置");
                }
            }
            foreach (var t in trans)
            {
                if (!t.Users.IsNullOrEmpty())
                {
                    t.Users = t.Users.OrderBy(o => o.OrgRank).OrderBy(o => o.Rank).ToList();
                }
            }
            return trans;
        }


        private bool ExecCommand(string command, WorkflowNextStepParameter runParameter)
        {
            WorkflowHelper helper = new WorkflowHelper(runParameter.CurrentUserId);
            helper.OpenWorkflow(runParameter.InstanceId, runParameter.ItemId);
            if (!string.IsNullOrEmpty(runParameter.OpinionContent))
            {
                helper.SetOpinion(HttpUtility.HtmlEncode(runParameter.OpinionContent), runParameter.OpinionArea);
            }
            bool result = false;
            switch (command.ToLower())
            {
                case "run":
                    var form = GetForm(helper) as IFlowFormService;
                    if (form == null || form.CanRunFlow())
                    {
                        var p = runParameter as WorkflowRunParameter;
                        helper.Run(p.RunContext());
                        result = true;
                    }
                    break;
                case "rollback":
                    result = helper.Rollback();
                    break;
                case "pushback":
                    result = helper.Pushback();
                    break;
                case "backtoroot":
                    result = helper.PushbackToRoot();
                    break;
                case "forceend":
                    result = helper.ForceToEnd();
                    break;
                case "switch":
                    result = false;
                    break;
                case "delete":
                    result = false;
                    break;
            }
            return result;
        }
    }
}