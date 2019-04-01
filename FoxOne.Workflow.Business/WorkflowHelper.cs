/*********************************************************
 * 作　　者：刘海峰
 * 联系邮箱：mailTo:liuhf@foxone.com
 * 创建时间：2014/12/29 17:24:46
 * 描述说明：
 * *******************************************************/
using System;
using System.Collections.Generic;
using FoxOne.Workflow.Kernel;
using FoxOne.Core;
using FoxOne.Workflow.DataAccess;
using System.Security.Principal;
using FoxOne.Workflow.Business;
using System.Linq;
using System.Transactions;
using FoxOne.Data;
namespace FoxOne.Workflow.Business
{
    /// <summary>
    /// 工作流API帮助类
    /// </summary>
    public partial class WorkflowHelper : IDisposable
    {
        public const string FORCE_END_LABEL = "强制结束";

        private IWorkflowInstanceService _instanceService;
        private IWorkflowInstanceService instanceService
        {
            get
            {
                return _instanceService ?? (_instanceService = ObjectHelper.GetObject<IWorkflowInstanceService>());
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="loginId"></param>
        public WorkflowHelper(string loginId)
        {
            CurrentUser = DBContext<IUser>.Instance.FirstOrDefault(o => o.LoginId.Equals(loginId, StringComparison.OrdinalIgnoreCase) || o.Id.Equals(loginId, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="user"></param>
        public WorkflowHelper(IPrincipal user)
            : this(user.Identity.Name)
        {

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="user"></param>
        public WorkflowHelper(IUser user)
        {
            CurrentUser = user;
        }

        /// <summary>
        /// 当前工作项
        /// </summary>
        public IWorkflowItem CurrentItem { get; private set; }

        /// <summary>
        /// 当前步骤
        /// </summary>
        public IActivity CurrentActivity { get; private set; }

        /// <summary>
        /// 当前流程对象
        /// </summary>
        public IWorkflow CurrentWorkflow { get; private set; }

        /// <summary>
        /// 当前用户
        /// </summary>
        public IUser CurrentUser { get; private set; }

        /// <summary>
        /// 当前流程实例
        /// </summary>
        public IWorkflowInstance FlowInstance
        {
            get;
            private set;
        }

        /// <summary>
        /// 发起流程
        /// </summary>
        /// <param name="appCode">流程应用编号</param>
        /// <param name="procName">流程实例名称</param>
        /// <param name="dataLocator">表单主键</param>
        /// <param name="impoLevel">缓急</param>
        /// <param name="secret">密级</param>
        public void StartWorkflow(string appCode, string procName, string dataLocator, int impoLevel = 0, int secret = 0)
        {
            CurrentWorkflow = Build(appCode);
            FlowInstance = GetNewInstance(appCode, procName, dataLocator, impoLevel, secret);
            using (TransactionScope tran = new TransactionScope())
            {
                CurrentWorkflow.AddInstance(FlowInstance);
                CurrentItem = GetNewWorkItem(CurrentWorkflow.Root, CurrentUser, appCode, FlowInstance.Id);
                FlowInstance.InsertWorkItem(CurrentItem);
                CurrentActivity = CurrentWorkflow.Root;
                tran.Complete();
            }
        }

        /// <summary>
        /// 更新流程实例状态
        /// </summary>
        /// <param name="procName">流程实例名称</param>
        /// <param name="dataLocator">表单主键</param>
        /// <param name="impoLevel">缓急</param>
        /// <param name="secret">密级</param>
        public void UpdateInstance(string procName, string dataLocator, int impoLevel, int secret)
        {
            Validate(false);
            FlowInstance.InstanceName = procName;
            FlowInstance.DataLocator = dataLocator;
            FlowInstance.ImportantLevel = impoLevel;
            FlowInstance.SecretLevel = secret;
            CurrentWorkflow.UpdateInstance(FlowInstance);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="appCode"></param>
        /// <returns></returns>
        public IWorkflow Build(string appCode)
        {
            var app = GetApplication(appCode);
            string workflowId = app == null ? "1" : app.WorkflowId;//测试时，未定义应用。
            var returnValue = ObjectHelper.GetObject<IWorkflowBuilder>().Build(workflowId);
            ValidateWorkflow(returnValue);
            return returnValue;
        }

        /// <summary>
        /// 根据流程应用ID获取流程应用详情
        /// </summary>
        /// <param name="applicationId">流程应用ID</param>
        /// <returns></returns>
        public static IWorkflowApplication GetApplication(string applicationId)
        {
            return DBContext<IWorkflowApplication>.Instance.Get(applicationId);
        }

        /// <summary>
        /// 打开流程实例
        /// </summary>
        /// <param name="dataLocator">表单主键</param>
        public void OpenWorkflow(string dataLocator)
        {
            var instance = instanceService.GetInstanceByDataLocator(dataLocator);
            if (instance == null)
            {
                throw new FoxOneException("不存在datalocator为：{0}的流程实例", dataLocator);
            }
            OpenWorkflow(instance, 0);
        }

        /// <summary>
        /// 打开流程实例
        /// </summary>
        /// <param name="instanceId">流程实例号</param>
        /// <param name="itemId">流程工作项ID</param>
        public void OpenWorkflow(string instanceId, int itemId)
        {
            var instance = instanceService.Get(instanceId);
            if (instance == null)
            {
                throw new FoxOneException("不存在实例号为：{0}的流程实例", instanceId);
            }
            OpenWorkflow(instance, itemId);
        }

        /// <summary>
        /// 清除流程缓存
        /// </summary>
        /// <param name="definitionId"></param>
        public static void ClearCache(string definitionId)
        {
            ObjectHelper.GetObject<IWorkflowBuilder>().ClearCache(definitionId);
        }

        /// <summary>
        /// 获取下一步可选步骤与可选用户
        /// </summary>
        /// <returns></returns>
        public List<NextStep> GetNextStep()
        {
            Validate(false);
            List<NextStep> returnValue = new List<NextStep>();
            IWorkflowContext context = GetWorkflowContext();
            var trans = GetAvailableTransitions(context);
            foreach (var tran in trans)
            {
                var nextStep = new NextStep()
                {
                    Label = tran.Label,
                    LabelDescription = tran.Description,
                    StepName = tran.To.Name,
                    Rank = tran.Rank,
                    NeedUser = true
                };
                GetNextStepUser(nextStep, tran.To, context);
                returnValue.Add(nextStep);
            }
            return returnValue.OrderBy(o => o.Rank).ToList();
        }

        /// <summary>
        /// 获取所有步骤
        /// </summary>
        /// <returns></returns>
        public List<NextStep> GetAllStep()
        {
            Validate(false);
            List<NextStep> returnValue = new List<NextStep>();
            IWorkflowContext context = GetWorkflowContext();
            foreach (var acti in CurrentWorkflow.Activities)
            {
                if ((acti is ResponseActivity) || (acti is EndActivity))
                {
                    var nextStep = new NextStep()
                    {
                        Label = acti.Alias,
                        LabelDescription = "",
                        StepName = acti.Name,
                        NeedUser = true
                    };
                    returnValue.Add(nextStep);
                }
            }
            return returnValue.OrderBy(o => o.Rank).ToList();
        }

        private void GetNextStepUser(NextStep nextStep, IActivity activity, IWorkflowContext context)
        {
            IActor actor = activity.Actor;
            nextStep.Users = new List<NextStepUser>();
            if (activity is ResponseActivity)
            {
                nextStep.MultipleSelectTag = (activity as ResponseActivity).MultipleSelectTag;
            }
            if (actor is UserSelectActor)
            {
                UserSelectActor userSelectActor = actor as UserSelectActor;
                nextStep.AllowFree = userSelectActor.AllowFree;
                nextStep.OnlySingleSel = userSelectActor.OnlySingleSelect;
                nextStep.AutoSelectAll = userSelectActor.AutoSelectAll;
                nextStep.AllowSelect = true;
                actor = userSelectActor.InnerActor;
                actor.Owner = activity;
            }
            else
            {
                nextStep.AllowFree = false;
                nextStep.AutoSelectAll = true;
                nextStep.AllowSelect = false;
                nextStep.OnlySingleSel = false;
            }
            if (actor != null)
            {
                try
                {
                    var groupByActors = actor.Resolve(context).GroupBy(o => o.DepartmentId);
                    foreach (var actors in groupByActors)
                    {
                        foreach (IUser u in actors.OrderBy(o => o.Rank))
                        {
                            nextStep.Users.Add(new NextStepUser() { StepName = activity.Name, ID = u.Id, Avatar = u.Avatar, Name = u.Name, OrgId = u.Department.Id, OrgName = u.Department.Name, Rank = u.Rank, OrgRank = u.Department.Rank });
                        }
                    }
                }
                catch (Exception ex) { nextStep.Message = ex.Message; }
            }
            if (activity is EndActivity)
            {
                nextStep.NeedUser = false;
            }
        }

        /// <summary>
        /// 将特定字符串转换成用于运行流程的参数
        /// </summary>
        /// <param name="userChoice">格式：userId1_orgId1_stepName1,userId2_orgId2_stepName2...</param>
        /// <returns></returns>
        public IList<IWorkflowChoice> GetUserChoice(string userChoice)
        {
            IList<IWorkflowChoice> choices = new List<IWorkflowChoice>();
            string[] tempChoice = userChoice.Split(',');
            foreach (var c in tempChoice)
            {
                var tempUser = c.Split('_');
                var choice = choices.FirstOrDefault(o => o.Choice == tempUser[2]);
                if (tempUser[0].Equals("NULL", StringComparison.CurrentCultureIgnoreCase))
                {
                    var workflowChoice = ObjectHelper.GetObject<IWorkflowChoice>();
                    workflowChoice.Choice = tempUser[2];
                    workflowChoice.Participant = new List<IUser>();

                    choices.Add(workflowChoice);
                }
                else
                {
                    var user = DBContext<IUser>.Instance.Get(tempUser[0]);
                    user.DepartmentId = tempUser[1];
                    int rank = 0;
                    if (int.TryParse(tempUser[3], out rank))
                    {
                        user.Rank = rank;
                    }
                    if (choice == null)
                    {
                        var workflowChoice = ObjectHelper.GetObject<IWorkflowChoice>();
                        workflowChoice.Choice = tempUser[2];
                        workflowChoice.Participant = new List<IUser>() { user };
                        choices.Add(workflowChoice);
                    }
                    else
                    {
                        choice.Participant.Add(user);
                    }
                }
            }
            return choices;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="stepName"></param>
        /// <param name="user"></param>
        /// <returns></returns>
        public IList<IWorkflowChoice> GetUserChoice(string stepName, NextStepUser user)
        {
            var returnValue = new List<IWorkflowChoice>();
            var choice = ObjectHelper.GetObject<IWorkflowChoice>();
            choice.Choice = stepName;
            choice.Participant = new List<IUser>();
            var tempUser = DBContext<IUser>.Instance.Get(user.ID);
            tempUser.DepartmentId = user.OrgId;
            choice.Participant.Add(tempUser);
            returnValue.Add(choice);
            return returnValue;
        }

        /// <summary>
        /// 运行流程
        /// </summary>
        /// <param name="userChoice"></param>
        public void Run(IList<IWorkflowChoice> userChoice = null)
        {
            Validate();
            var context = GetWorkflowContext();
            if (userChoice != null)
            {
                context.UserChoice = userChoice;
            }
            CurrentWorkflow.Run(context);
        }

        /// <summary>
        /// 删除当前流程实例
        /// </summary>
        public void DeleteWorkflow()
        {
            Validate(false);

            using (var tran = new TransactionScope())
            {

                FlowInstance.DeleteWorkItem();
                CurrentWorkflow.DeleteInstance(FlowInstance);
                tran.Complete();
            }
            Log("删除了流程实例（包括相关联的工作项及流程参数）");
        }

        /// <summary>
        /// 设置流程流转参数
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public void SetParameter(string key, string value)
        {
            Validate(false);
            if (!FlowInstance.Parameters.ContainsKey(key) || !FlowInstance.Parameters[key].Equals(value))
            {
                FlowInstance.SetParameter(key, value);
                //Log(string.Format("设置了流程参数key:{0},value:{1}", key, value));
            }
        }

        private void Log(string operation)
        {
            Logger.Info(string.Format("Workflow:{0}:{1} -- 操作人:{2}", FlowInstance.Id, operation, CurrentUser.Name));
        }

        /// <summary>
        /// 设置审批意见
        /// </summary>
        /// <param name="opinionContent">意见内容</param>
        /// <param name="opinionArea">意见区域ID</param>
        public void SetOpinion(string opinionContent, int opinionArea)
        {
            Validate(false);
            CurrentItem.OpinionContent = opinionContent;
            CurrentItem.OpinionType = opinionArea;
            FlowInstance.UpdateWorkItem(CurrentItem);
        }

        /// <summary>
        /// 是否需要显示用户选择界面
        /// </summary>
        /// <returns></returns>
        public bool ShowUserSelect()
        {
            Validate(false);
            if (CurrentActivity is ResponseActivity && (CurrentActivity as ResponseActivity).NeedChoice)
            {
                var context = GetWorkflowContext();
                return (CurrentActivity as ResponseActivity).ShowUserSelect(context);
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// 跳转流程到指定步骤 
        /// </summary>
        /// <param name="userChoice"></param>
        public void Switch(IWorkflowChoice userChoice)
        {
            Validate();
            if (userChoice == null || string.IsNullOrEmpty(userChoice.Choice))
            {
                throw new Exception(string.Format("请选择要跳转到的步骤", userChoice.Choice));
            }
            var targetActi = CurrentWorkflow[userChoice.Choice];
            if (targetActi == null)
            {
                throw new Exception(string.Format("流程定义中不存在名为【{0}】的步骤", userChoice.Choice));
            }
            if (!(targetActi is ResponseActivity))
            {
                throw new Exception("不允许跳转到非审批步骤中");
            }
            using (var tran = new TransactionScope())
            {
                SetAutoFinished("跳转流程");
                var context = GetWorkflowContext();
                context.LevelCode = "00";
                context.UserChoice = new List<IWorkflowChoice>() { userChoice };
                targetActi.Enter(context);
                tran.Complete();
            }
            Log(string.Format("跳转了流程，跳转到步骤【{0}】", userChoice.Choice));
        }

        public void Dispose()
        {

        }

        /// <summary>
        /// 强制结束当前审批流
        /// </summary>
        /// <returns></returns>
        public bool ForceToEnd()
        {
            Validate();
            if (CurrentItem.PreItemId == 0)
            {
                throw new FoxOneException("开始步骤不允许此操作！");
            }
            using (TransactionScope tran = new TransactionScope())
            {
                SetAutoFinished(FORCE_END_LABEL);
                CurrentItem.Status = WorkItemStatus.ForceEnd;
                FlowInstance.UpdateWorkItem(CurrentItem);
                var context = GetWorkflowContext();
                context.LevelCode = "00";
                context.FlowInstance.Description = FORCE_END_LABEL;
                var endActivity = new EndActivity() { Name = FORCE_END_LABEL, Alias = FORCE_END_LABEL };
                endActivity.Owner = CurrentWorkflow;
                endActivity.Enter(context);
                tran.Complete();
            }
            Log("强制结束了流程");
            return true;
        }

        /// <summary>
        /// 暂停流程
        /// </summary>
        public void Pause()
        {
            Validate();
            FlowInstance.FlowTag = FlowStatus.Pause;
            CurrentWorkflow.UpdateInstance(FlowInstance);
        }

        /// <summary>
        /// 恢复流程
        /// </summary>
        public void Recovery()
        {
            Validate(false);
            FlowInstance.FlowTag = FlowStatus.Running;
            CurrentWorkflow.UpdateInstance(FlowInstance);
        }

        /// <summary>
        /// 退回当前工作项到上一步
        /// </summary>
        /// <returns></returns>
        public bool Pushback()
        {
            Validate();
            if (CurrentItem.PreItemId == 0)
            {
                throw new FoxOneException("开始步骤不允许回退上一步！");
            }
            var preTask = FlowInstance.WorkItems.FirstOrDefault(o => o.ItemId == CurrentItem.PreItemId);
            if (preTask == null)
            {
                throw new FoxOneException("上一工作项为空，不能执行该操作!");
            }
            if (preTask.PartUserName.Equals("系统", StringComparison.CurrentCultureIgnoreCase)
                || preTask.LevelCode != CurrentItem.LevelCode
                || preTask.Status == WorkItemStatus.AutoFinished)
            {
                throw new FoxOneException("不允许回退到系统处理步骤或跨越会签内外步骤");
            }
            var workItems = FlowInstance.WorkItems.Where(o => o.PreItemId == CurrentItem.PreItemId).ToList();
            var updateItems = new List<IWorkflowItem>();
            if (workItems.Count > 1)
            {
                //上一工作项处理人员发送了多人处理
                foreach (var item in workItems)
                {
                    if (item.Status != WorkItemStatus.Finished && item.ItemId != CurrentItem.ItemId)
                    {
                        item.Status = WorkItemStatus.AutoFinished;
                        item.FinishTime = DateTime.Now;
                        item.AssigneeUserId = CurrentUser.Id;
                        item.AssigneeUserName = CurrentUser.Name;
                        item.AutoFinish = true;
                        item.UserChoice = "回退上一步";
                        updateItems.Add(item);
                    }
                }
            }
            CurrentItem.FinishTime = DateTime.Now;
            CurrentItem.UserChoice = "回退上一步";
            CurrentItem.Status = WorkItemStatus.Pushback;
            CurrentItem.AutoFinish = false;
            updateItems.Add(CurrentItem);
            IUser user = DBContext<IUser>.Instance.Get(preTask.PartUserId);
            var newItem = GetNewWorkItem(CurrentWorkflow[preTask.CurrentActivity], user, CurrentItem.AppCode, CurrentItem.InstanceId);
            var newestItem = FlowInstance.WorkItems.OrderByDescending(o => o.ItemId).First();
            newItem.ItemId = newestItem.ItemId + 1;
            newItem.ItemSeq = newestItem.ItemSeq + 1;
            newItem.PreItemId = preTask.PreItemId;
            newItem.PasserUserId = preTask.PasserUserId;
            newItem.PasserUserName = preTask.PasserUserName;
            newItem.LevelCode = preTask.LevelCode;
            newItem.ParallelInfo = preTask.ParallelInfo;
            using (var tran = new TransactionScope())
            {
                FlowInstance.UpdateWorkItem(updateItems);
                FlowInstance.InsertWorkItem(newItem);
                tran.Complete();
            }
            Log(string.Format("退回了流程到步骤【{0}】", preTask.CurrentActivity));
            return true;
        }

        /// <summary>
        /// 撤回已发送的工作项
        /// </summary>
        /// <returns></returns>
        public bool Rollback()
        {
            Validate(false);
            var workItems = FlowInstance.WorkItems.Where(o => o.PreItemId == CurrentItem.ItemId).ToList();
            if (workItems.Count(o => o.Status >= WorkItemStatus.Finished) > 0)
            {
                throw new FoxOneException("后续工作项已有完成，不允许执行撤回操作！");
            }
            foreach (var item in workItems)
            {
                item.Status = WorkItemStatus.BeRollBack;
                item.AssigneeUserId = CurrentUser.Id;
                item.AssigneeUserName = CurrentUser.Name;
                item.FinishTime = DateTime.Now;
                item.AutoFinish = true;
                item.UserChoice = "被撤回";
            }
            using (var tran = new TransactionScope())
            {

                FlowInstance.UpdateWorkItem(workItems);
                CurrentItem.Status = WorkItemStatus.RollBack;
                FlowInstance.UpdateWorkItem(CurrentItem);
                IUser user = DBContext<IUser>.Instance.Get(CurrentItem.PartUserId);
                var newItem = GetNewWorkItem(CurrentWorkflow[CurrentItem.CurrentActivity], user, CurrentItem.AppCode, CurrentItem.InstanceId);
                var newestItem = FlowInstance.WorkItems.OrderByDescending(o => o.ItemId).First();
                newItem.ItemId = newestItem.ItemId + 1;
                newItem.ItemSeq = newestItem.ItemSeq + 1;
                newItem.PasserUserId = CurrentItem.PasserUserId;
                newItem.PasserUserName = CurrentItem.PasserUserName;
                newItem.LevelCode = CurrentItem.LevelCode;
                newItem.ParallelInfo = CurrentItem.ParallelInfo;
                newItem.PreItemId = CurrentItem.PreItemId;
                FlowInstance.InsertWorkItem(newItem);
                tran.Complete();
            }
            Log(string.Format("在步骤【{0}】撤回了流程", CurrentItem.CurrentActivity));
            return true;
        }

        /// <summary>
        /// 退回拟稿人
        /// </summary>
        /// <returns></returns>
        public bool PushbackToRoot()
        {
            Validate();
            if (CurrentItem.PreItemId == 0)
            {
                throw new FoxOneException("开始步骤不允许退回拟稿人！");
            }
            if (CurrentActivity == CurrentWorkflow.Root)
            {
                throw new FoxOneException("当前已是拟稿步骤！");
            }
            using (var tran = new TransactionScope())
            {
                SetAutoFinished("退回拟稿人");
                CurrentItem.Status = WorkItemStatus.BackToRoot;
                FlowInstance.UpdateWorkItem(CurrentItem);
                var context = GetWorkflowContext();
                context.LevelCode = "00";
                CurrentWorkflow.Root.Enter(context);
                tran.Complete();
            }
            Log("退回拟稿人");
            return true;
        }

        /// <summary>
        /// 退回指定步骤
        /// </summary>
        /// <param name="itemId"></param>
        public void BackToActivity(int itemId)
        {
            Validate(false);
            var item = FlowInstance.WorkItems.FirstOrDefault(o => o.ItemId == itemId);
            if (item == null || item.PartUserName == "系统" || item.Alias == "传阅" || item.LevelCode != "00")
            {
                throw new FoxOneException("暂不支持回到传阅步骤、自动步骤及会签内步骤");
            }
            using (TransactionScope tran = new TransactionScope())
            {
                FlowInstance.BackToTask(itemId);
                instanceService.BackToRunning(FlowInstance.Id, itemId, item.ItemSeq, item.CurrentActivity);
                tran.Complete();
            }
        }

        /// <summary>
        /// 传阅
        /// </summary>
        /// <param name="userIds"></param>
        public void SendToOtherToRead(IList<string> userIds)
        {
            SendToOtherToRead(string.Join(",", userIds.ToArray()));
        }

        /// <summary>
        /// 传阅
        /// </summary>
        /// <param name="userIds">用户ID，多个用逗号隔开</param>
        public void SendToOtherToRead(string userIds)
        {
            Validate(false);
            var userIdSplit = userIds.Split(new char[] { ',', '|', ';' }, StringSplitOptions.RemoveEmptyEntries);
            var users = DBContext<IUser>.Instance.Where(o => userIdSplit.Contains(o.Id, StringComparer.OrdinalIgnoreCase)).ToList();
            int taskId = FlowInstance.GetMaxReadTaskID();
            using (var tran = new TransactionScope())
            {
                foreach (var user in users)
                {
                    var newItem = ObjectHelper.GetObject<IWorkflowItem>("Read");
                    newItem.InstanceId = CurrentItem.InstanceId;
                    newItem.ReceiveTime = DateTime.Now;
                    newItem.PartUserId = user.Id;
                    newItem.PartUserName = user.Name;
                    newItem.PartDepartmentId = user.Department.Id;
                    newItem.PartDepepartmentName = user.Department.Name;
                    newItem.Status = WorkItemStatus.Sent;
                    newItem.ItemId = ++taskId;
                    newItem.Alias = "传阅";
                    newItem.CurrentActivity = "传阅";
                    newItem.AppCode = CurrentItem.AppCode;
                    newItem.PreItemId = CurrentItem.ItemId;
                    newItem.PasserUserId = CurrentUser.Id;
                    newItem.PasserUserName = CurrentUser.Name;
                    FlowInstance.InsertWorkItem(newItem);
                }
                tran.Complete();
            }
        }

        #region 私有方法

        private void OpenWorkflow(IWorkflowInstance instance, int itemId)
        {
            if (itemId == 0)
            {
                var items = instance.WorkItems.Where(o => o.PartUserId != null && o.PartUserId.Equals(CurrentUser.Id, StringComparison.OrdinalIgnoreCase));
                if (items.IsNullOrEmpty())
                {
                    items = instance.WorkItemsRead.Where(o => o.PartUserId != null && o.PartUserId.Equals(CurrentUser.Id, StringComparison.OrdinalIgnoreCase));
                }

                if (!items.IsNullOrEmpty())
                {
                    CurrentItem = items.OrderByDescending(o => o.ItemId).First();
                }
                else
                {
                    CurrentItem = instance.WorkItems.FirstOrDefault(o => o.ItemId == 1);
                }
            }
            else
            {
                if (itemId >= 10000)
                {
                    CurrentItem = instance.WorkItemsRead.FirstOrDefault(o => o.ItemId == itemId);
                }
                else
                {
                    CurrentItem = instance.WorkItems.FirstOrDefault(o => o.ItemId == itemId);
                }
            }
            if (CurrentItem == null)
            {
                throw new FoxOneException("不存在ItemId为{0}的工作项".FormatTo(itemId));
            }
            FlowInstance = instance;
            CurrentWorkflow = Build(FlowInstance.ApplicationId);
            if (CurrentItem.CurrentActivity != "传阅" && CurrentItem.CurrentActivity != WorkflowHelper.FORCE_END_LABEL)
            {
                CurrentActivity = CurrentWorkflow[CurrentItem.CurrentActivity];
                if (CurrentActivity == null)
                {
                    throw new FoxOneException(string.Format("流程定义中不存在步骤名为{0}的步骤", CurrentItem.CurrentActivity));
                }
            }
        }

        private IList<ITransition> GetAvailableTransitions(IWorkflowContext context)
        {
            IList<ITransition> result = new List<ITransition>();
            foreach (var tran in CurrentActivity.Transitions)
            {
                if (tran.PreResolve(context))
                {
                    if (tran.To is ParallelStartActivity || tran.To is ParallelEndActivity)
                    {
                        foreach (var tran1 in tran.To.Transitions)
                        {
                            if (tran1.PreResolve(context))
                            {
                                result.Add(tran1);
                            }
                        }
                    }
                    else
                    {
                        result.Add(tran);
                    }
                }
            }
            return result;
        }
        private IWorkflowInstance GetNewInstance(string appCode, string procName, string dataLocator, int impoLevel, int secret)
        {
            var instance = ObjectHelper.GetObject<IWorkflowInstance>();
            instance.ApplicationId = appCode;
            instance.CreatorId = CurrentUser.Id;
            instance.WorkItemNewSeq = 1;
            instance.WorkItemNewTask = 1;
            instance.StartTime = DateTime.Now;
            instance.FlowTag = FlowStatus.Begin;
            instance.SecretLevel = secret;
            instance.InstanceName = string.IsNullOrEmpty(procName) ? instance.Application.Name : procName;
            instance.ImportantLevel = impoLevel;
            instance.DataLocator = dataLocator;
            return instance;
        }

        private IWorkflowItem GetNewWorkItem(IActivity activity, IUser user, string appCode, string instanceId)
        {
            var newItem = ObjectHelper.GetObject<IWorkflowItem>();
            newItem.AppCode = appCode;
            newItem.Alias = activity.Alias;
            newItem.CurrentActivity = activity.Name;
            newItem.ItemSeq = 1;
            newItem.ItemId = 1;
            newItem.Status = WorkItemStatus.Sent;
            newItem.InstanceId = instanceId;
            newItem.ReceiveTime = DateTime.Now;
            if (activity is ResponseActivity)
            {
                newItem.ExpiredTime = (activity as ResponseActivity).GetExpiredTime();
            }
            newItem.PartUserId = user.Id;
            newItem.PartUserName = user.Name;
            newItem.LevelCode = "00";
            newItem.PartDepartmentId = user.Department.Id;
            newItem.PartDepepartmentName = user.Department.Name;
            return newItem;
        }

        /// <summary>
        /// 检查流程定义是否有效
        /// </summary>
        /// <param name="workflow"></param>
        private void ValidateWorkflow(IWorkflow workflow)
        {
            foreach (var acti in workflow.Activities)
            {
                if (!(acti is EndActivity) && !(acti is BreakdownActivity))
                {
                    if (acti.Transitions == null || acti.Transitions.Count == 0)
                    {
                        throw new FoxOneException(string.Format("流程定义错误 ，步骤【{0}】没有向外的迁移", acti.Name));
                    }
                }
                if (!ExistTranToActi(workflow, acti))
                {
                    throw new FoxOneException(string.Format("流程定义错误，没有指向步骤【{0}】的迁移", acti.Name));
                }
            }
            foreach (var tran in workflow.Transitions)
            {
                if (tran.To == null || tran.From == null)
                {
                    throw new FoxOneException("无效迁移");
                }
            }
        }

        /// <summary>
        /// 是否存在指向某步骤的迁移
        /// </summary>
        /// <param name="workflow"></param>
        /// <param name="acti"></param>
        /// <returns></returns>
        private bool ExistTranToActi(IWorkflow workflow, IActivity acti)
        {
            //如果该步骤是开始步骤，则无指向该步骤的迁移也正常。
            if (acti == workflow.Root) return true;
            foreach (var tran in workflow.Transitions)
            {
                if (tran.To == acti)
                {
                    return true;
                }
            }
            return false;
        }
        private IWorkflowContext GetWorkflowContext()
        {
            var context = ObjectHelper.GetObject<IWorkflowContext>();
            context.LevelCode = CurrentItem.LevelCode;
            context.CurrentUser = CurrentUser;
            context.FlowInstance = FlowInstance;
            context.CurrentTask = CurrentItem;
            context.Parameter = FlowInstance.Parameters;
            return context;
        }
        private void Validate(bool checkIsRunning = true)
        {
            if (CurrentWorkflow == null || CurrentItem == null || FlowInstance == null)
            {
                throw new FoxOneException("未打开流程，或工作项不存在");
            }
            if (checkIsRunning)
            {
                if (FlowInstance.FlowTag >= FlowStatus.Finished)
                {
                    throw new FoxOneException(string.Format("当前流程处于【{0}】状态，无法继续流转。", FlowInstance.FlowTag.ToString()));
                }
                if (CurrentItem.Status >= WorkItemStatus.Finished)
                {
                    throw new FoxOneException("当前工作项已结束，不能执行该操作!");
                }
            }
        }
        private void SetAutoFinished(string reason)
        {
            var unFinishedItem = FlowInstance.WorkItems.Where(o => o.Status < WorkItemStatus.Finished).ToList();
            foreach (var item in unFinishedItem)
            {
                item.Status = WorkItemStatus.AutoFinished;
                item.AssigneeUserId = CurrentUser.Id;
                item.AssigneeUserName = CurrentUser.Name;
                item.FinishTime = DateTime.Now;
                item.AutoFinish = true;
                item.UserChoice = reason;
            }
            FlowInstance.UpdateWorkItem(unFinishedItem);
        }
        #endregion

        /// <summary>
        /// 设置阅读时间为当前
        /// </summary>
        public void SetReadTime()
        {
            Validate(false);
            var task = CurrentItem;
            if (task.Status == WorkItemStatus.Sent)
            {
                task.Status = WorkItemStatus.Readed;
                task.ReadTime = DateTime.Now;
                if (task.ItemId >= 10000)
                {
                    task.FinishTime = DateTime.Now;
                    task.Status = WorkItemStatus.Finished;
                }
                FlowInstance.UpdateWorkItem(task);
            }
        }

        /// <summary>
        /// 根据用户ID获取待办事项
        /// </summary>
        /// <param name="partUserId">用户ID</param>
        /// <returns></returns>
        public static IList<ToDoList> GetToDoList(string partUserId)
        {
            return CacheHelper.GetFromCache<IList<ToDoList>>("WorkflowToDoListCache_{0}".FormatTo(partUserId), () =>
            {
                var items = Dao.Get().Query<WorkflowItem>().Where(o => o.PartUserId == partUserId && o.Status < WorkItemStatus.Finished).ToList();
                return GetListInner(items);
            }, DateTime.Now.AddSeconds(5));
        }

        private static IList<ToDoList> GetListInner(IList<WorkflowItem> items)
        {
            var result = new List<ToDoList>();
            if (items.IsNullOrEmpty()) return result;
            var itemIds = items.Distinct(o => o.InstanceId).Select(o => o.InstanceId).ToArray();
            var ins = Dao.Get().Query<WorkflowInstance>().Where(o => itemIds.Contains(o.Id)).ToList();
            var itemDESC = items.OrderByDescending(o => o.ItemId);
            foreach (var ii in itemDESC)
            {
                if (result.Any(o => o.InstanceId.Equals(ii.InstanceId, StringComparison.OrdinalIgnoreCase)))
                {
                    continue;
                }
                var item = ins.FirstOrDefault(o => o.Id.Equals(ii.InstanceId, StringComparison.OrdinalIgnoreCase));
                var todo = new ToDoList()
                {
                    InstanceId = item.Id,
                    InstanceCreateTime = item.StartTime.Value,
                    InstanceCreator = item.Creator.Name,
                    ApplicationId = item.Application.Id,
                    ApplicationName = item.Application.Name,
                    ApplicationType = item.Application.Type,
                    CurrentActivityAlias = item.CurrentActivityName,
                    CurrentActivityName = ii.CurrentActivity,
                    ExpiredTime = ii.ExpiredTime,
                    FinishTime = ii.FinishTime,
                    InstanceName = item.InstanceName,
                    InstanceStatus = item.FlowTag.GetDescription(),
                    ItemId = ii.ItemId,
                    ItemStatus = ii.Status.GetDescription(),
                    PartUserId = ii.PartUserId,
                    PartUserName = ii.PartUserName,
                    PasserUserId = ii.PasserUserId,
                    PasserUserName = ii.PasserUserName,
                    ReadTime = ii.ReadTime,
                    ReceiveTime = ii.ReceiveTime,
                    DataLocator = item.DataLocator,
                    Avatar=item.Creator.Avatar.IsNullOrEmpty()?"/iamges/{0}.png".FormatTo(item.Creator.Sex):item.Creator.Avatar,
                    Description=item.Description
                };
                result.Add(todo);
            }
            return result;
        }

        /// <summary>
        /// 根据用户ID获取已办事项
        /// </summary>
        /// <param name="partUserId">用户ID</param>
        /// <returns></returns>
        public static IList<ToDoList> GetDoneList(string partUserId)
        {
            var items = Dao.Get().Query<WorkflowItem>().Where(o => o.PartUserId == partUserId &&
           (o.Status == WorkItemStatus.Finished ||
           o.Status == WorkItemStatus.RollBack ||
           o.Status == WorkItemStatus.Pushback ||
           o.Status == WorkItemStatus.BackToRoot ||
           o.Status == WorkItemStatus.ForceEnd)).ToList();
            return GetListInner(items);
        }

        /// <summary>
        /// 根据用户ID获取知会事项
        /// </summary>
        /// <param name="partUserId">用户ID</param>
        /// <returns></returns>
        public static IList<ToDoList> GetReadList(string partUserId)
        {
            var items = Dao.Get().Query<WorkflowItemRead>().Where(o => o.PartUserId == partUserId).ToList().ToList<WorkflowItem>();
            return GetListInner(items);
        }

        /// <summary>
        /// 获取所有流程实例
        /// </summary>
        /// <returns></returns>
        public static IList<IWorkflowInstance> GetAllInstance()
        {
            return DBContext<IWorkflowInstance>.Instance;
        }

        /// <summary>
        /// 获取所有流程应用
        /// </summary>
        /// <returns></returns>
        public static IList<IWorkflowApplication> GetAllApplication()
        {
            return DBContext<IWorkflowApplication>.Instance;
        }

        /// <summary>
        /// 获取所有流程定义
        /// </summary>
        /// <returns></returns>
        public static IList<IWorkflowDefinition> GetAllDefinition()
        {
            return DBContext<IWorkflowDefinition>.Instance;
        }
    }

    /// <summary>
    /// 下一步可选步骤
    /// </summary>
    public class NextStep
    {
        /// <summary>
        /// 多选标识
        /// </summary>
        public string MultipleSelectTag { get; set; }

        /// <summary>
        /// 步骤名称
        /// </summary>
        public string StepName { get; set; }

        /// <summary>
        /// 迁移标签
        /// </summary>
        public string Label { get; set; }

        /// <summary>
        /// 步骤排序
        /// </summary>
        public int Rank { get; set; }

        /// <summary>
        /// 迁移描述
        /// </summary>
        public string LabelDescription { get; set; }

        /// <summary>
        /// 是否需要选人
        /// </summary>
        public bool NeedUser { get; set; }

        /// <summary>
        /// 允许选择
        /// </summary>
        public bool AllowSelect { get; set; }

        /// <summary>
        /// 允许自由选人
        /// </summary>
        public bool AllowFree { get; set; }

        /// <summary>
        /// 只允许单选
        /// </summary>
        public bool OnlySingleSel { get; set; }

        /// <summary>
        /// 自动选中所有人
        /// </summary>
        public bool AutoSelectAll { get; set; }

        /// <summary>
        /// 当前步骤待选用户
        /// </summary>
        public List<NextStepUser> Users { get; set; }

        /// <summary>
        /// 异常消息
        /// </summary>
        public string Message { get; set; }
    }

    /// <summary>
    /// 下一步待选用户
    /// </summary>
    public class NextStepUser
    {
        /// <summary>
        /// 待选用户ID
        /// </summary>
        public string ID { get; set; }

        /// <summary>
        /// 待选用户名称
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 待选用户所属部门ID
        /// </summary>
        public string OrgId { get; set; }

        /// <summary>
        /// 待选用户所属部门名称
        /// </summary>
        public string OrgName { get; set; }

        /// <summary>
        /// 待选用户排序值
        /// </summary>
        public int Rank { get; set; }

        /// <summary>
        /// 待选用户所属部门排序值
        /// </summary>
        public int OrgRank { get; set; }

        /// <summary>
        /// 步骤名称
        /// </summary>
        public string StepName { get; set; }

        /// <summary>
        /// 待选用户头像
        /// </summary>
        public string Avatar { get; set; }
    }

    /// <summary>
    /// 待办已办事项信息
    /// </summary>
    public class ToDoList
    {
        /// <summary>
        /// 实例号
        /// </summary>
        public string InstanceId { get; set; }

        /// <summary>
        /// 实例名称
        /// </summary>
        public string InstanceName { get; set; }

        /// <summary>
        /// 实例创建者
        /// </summary>
        public string InstanceCreator { get; set; }

        /// <summary>
        /// 实例创建时间
        /// </summary>
        public DateTime InstanceCreateTime { get; set; }

        /// <summary>
        /// 实例状态：拟稿、运行中、结束
        /// </summary>
        public string InstanceStatus { get; set; }

        /// <summary>
        /// 表单主键
        /// </summary>
        public string DataLocator { get; set; }

        /// <summary>
        /// 流程应用号
        /// </summary>
        public string ApplicationId { get; set; }

        /// <summary>
        /// 流程应用名称
        /// </summary>
        public string ApplicationName { get; set; }

        /// <summary>
        /// 流程应用类型
        /// </summary>
        public string ApplicationType { get; set; }

        /// <summary>
        /// 工作项ID
        /// </summary>
        public int ItemId { get; set; }

        /// <summary>
        /// 工作项状态：送达、已读、完成
        /// </summary>
        public string ItemStatus { get; set; }

        /// <summary>
        /// 参与者办理的步骤
        /// </summary>
        public string CurrentActivityName { get; set; }

        /// <summary>
        /// 流程当前所在步骤
        /// </summary>
        public string CurrentActivityAlias { get; set; }

        /// <summary>
        /// 参与者ID
        /// </summary>
        public string PartUserId { get; set; }

        /// <summary>
        /// 参与者名称
        /// </summary>
        public string PartUserName { get; set; }

        /// <summary>
        /// 传递者ID
        /// </summary>
        public string PasserUserId { get; set; }

        /// <summary>
        /// 传递者名称
        /// </summary>
        public string PasserUserName { get; set; }

        /// <summary>
        /// 工作项接收时间
        /// </summary>
        public DateTime? ReceiveTime { get; set; }

        /// <summary>
        /// 工作项阅读时间
        /// </summary>
        public DateTime? ReadTime { get; set; }

        /// <summary>
        /// 工作项过期时间
        /// </summary>
        public DateTime? ExpiredTime { get; set; }

        /// <summary>
        /// 工作项完成时间
        /// </summary>
        public DateTime? FinishTime { get; set; }

        /// <summary>
        /// 待选用户头像
        /// </summary>
        public string Avatar { get; set; }

        /// <summary>
        /// 备注
        /// </summary>
        public string Description { get; set; }
    }
}
