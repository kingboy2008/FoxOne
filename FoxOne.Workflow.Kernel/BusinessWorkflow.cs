/*********************************************************
 * 作　　者：刘海峰
 * 联系邮箱：mailTo:liuhf@FoxOne.net
 * 创建时间：2015/1/26 14:14:26
 * 描述说明：
 * *******************************************************/
using FoxOne.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Transactions;

namespace FoxOne.Workflow.Kernel
{
    public class BusinessWorkflow : IWorkflow
    {
        private event WorkflowEventHandler _OnActivityEnter;
        private event WorkflowEventHandler _OnActivityExecute;
        private event WorkflowEventHandler _OnActivityExit;
        private object lockKey = new object();

        private DateTime d = DateTime.Now;

        public event WorkflowEventHandler OnActivityEnter
        {
            add
            {
                lock (lockKey)
                {
                    _OnActivityEnter = null;
                    _OnActivityEnter += value;

                }
            }
            remove
            {
                lock (lockKey)
                {
                    if (_OnActivityEnter != null)
                    {
                        _OnActivityEnter -= value;
                    }
                }
            }
        }

        public event WorkflowEventHandler OnActivityExecute
        {
            add
            {
                lock (lockKey)
                {
                    _OnActivityExecute = null;
                    _OnActivityExecute += value;

                }
            }
            remove
            {
                lock (lockKey)
                {
                    if (_OnActivityExecute != null)
                    {
                        _OnActivityExecute -= value;
                    }
                }
            }
        }

        public event WorkflowEventHandler OnActivityExit
        {
            add
            {
                lock (lockKey)
                {
                    _OnActivityExit = null;
                    _OnActivityExit += value;

                }
            }
            remove
            {
                lock (lockKey)
                {
                    if (_OnActivityExit != null)
                    {
                        _OnActivityExit -= value;
                    }
                }
            }
        }

        public virtual IActivity Root
        {
            get;
            set;
        }

        public virtual IList<IActivity> Activities
        {
            get;
            set;
        }

        public virtual IList<ITransition> Transitions
        {
            get;
            set;
        }

        public virtual IActivity this[string ActivityName]
        {
            get
            {
                foreach (var activity in Activities)
                {
                    if (activity.Name.Equals(ActivityName, StringComparison.CurrentCultureIgnoreCase))
                    {
                        return activity;
                    }
                }
                return null;
            }
        }

        public virtual string Name
        {
            get;
            set;
        }

        public virtual void AddInstance(IWorkflowInstance instance)
        {
            InstanceService.Insert(instance);
            WorkflowInstanceLog(instance, "新增");
        }

        private void WorkflowInstanceLog(IWorkflowInstance instance, string type)
        {
            Logger.Info(string.Format("Workflow:{0}:{1}流程实例：procName:{2} - creatorName:{3} - datalocator:{4} - flowtag:{5}",
                instance.Id,
                type,
                instance.InstanceName,
                instance.Creator.Name,
                instance.DataLocator,
                instance.FlowTag));
        }

        public virtual void UpdateInstance(IWorkflowInstance instance)
        {
            InstanceService.Update(instance);
            WorkflowInstanceLog(instance, "更新");
        }

        public virtual void DeleteInstance(IWorkflowInstance instance)
        {
            InstanceService.Delete(instance);
            WorkflowInstanceLog(instance, "删除");
        }

        private bool BoolExecute(string stage, Func<IWorkflowContext, bool> func, IWorkflowContext context, IActivity activity)
        {
            var result = func(context);
            Logger.Info("Workflow:{0}:执行步骤：{1} 的{2}方法，返回结果为：{3}", context.FlowInstance.Id, activity.Name, stage, result);
            return result;
        }

        private void VoidExecute(ActivityStep step, Action<IWorkflowContext> action, IWorkflowContext context, IActivity activity)
        {
            switch (step)
            {
                case ActivityStep.Enter:
                    _OnActivityEnter?.Invoke(context, activity);
                    break;
                case ActivityStep.Execute:
                    _OnActivityExecute?.Invoke(context, activity);
                    break;
                case ActivityStep.Exit:
                    _OnActivityExit?.Invoke(context, activity);
                    break;
                default:
                    break;
            }
            action(context);
        }

        protected virtual void InnerRun(IActivity activity, IWorkflowContext context)
        {
            string procID = context.FlowInstance.Id;
            if (!BoolExecute("CanExecute", activity.CanExecute, context, activity)) return;
            VoidExecute(ActivityStep.Execute, activity.Execute, context, activity);
            if (!BoolExecute("CanExit", activity.CanExit, context, activity)) return;
            VoidExecute(ActivityStep.Exit, activity.Exit, context, activity);
            if (activity.Transitions.IsNullOrEmpty()) return;
            foreach (var tran in activity.Transitions)
            {
                if (tran.Resolve(context))
                {
                    if (BoolExecute("CanEnter", tran.To.CanEnter, context, tran.To))
                    {
                        VoidExecute(ActivityStep.Enter, tran.To.Enter, context, tran.To);
                        if (tran.To.AutoRun)
                        {
                            InnerRun(tran.To, context);
                        }
                    }
                }
            }
        }

        public virtual void Run(IWorkflowContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException("流程运行需要上下文");
            }
            if (context.FlowInstance.FlowTag >= FlowStatus.Finished)
            {
                throw new FoxOneException(string.Format("当前流程处于【{0}】状态，无法继续流转。", context.FlowInstance.FlowTag.ToString()));
            }
            if (context.CurrentTask.Status >= WorkItemStatus.Finished)
            {
                throw new FoxOneException("当前工作项状态不允许运行");
            }
            using (TransactionScope tran = new TransactionScope())
            {
                if (context.FlowInstance.FlowTag == FlowStatus.Begin)
                {
                    context.FlowInstance.FlowTag = FlowStatus.Running;
                    UpdateInstance(context.FlowInstance);
                }
                InnerRun(this[context.CurrentTask.CurrentActivity], context);
                tran.Complete();
            }
        }

        private IWorkflowInstanceService instanceService;
        public IWorkflowInstanceService InstanceService
        {
            get
            {
                return instanceService ?? (instanceService = ObjectHelper.GetObject<IWorkflowInstanceService>());
            }
        }
    }

    public enum ActivityStep
    {
        Enter,
        Execute,
        Exit
    }

    public enum ItemActionType
    {
        Insert,
        Update,
        Delete
    }

    public static class WorkflowEventManager
    {
        private static IDictionary<string, Action<IWorkflowInstance, IWorkflowItem>> WorkItemEventList = new Dictionary<string, Action<IWorkflowInstance, IWorkflowItem>>();
        private const string KeyTemplate = "{0}_{1}";

        public static void RaiseWorkItemEvent(EventStep step, ItemActionType type, IWorkflowInstance instance, IWorkflowItem item)
        {
            string key = KeyTemplate.FormatTo(step.ToString(), type.ToString());
            if (WorkItemEventList.ContainsKey(key))
            {
                try
                {
                    Logger.Info("Workflow:{0}:Raise WorkflowItemEvent:{1}".FormatTo(instance.Id, key));
                    WorkItemEventList[key](instance, item);
                }
                catch (Exception ex)
                {
                    Logger.Error("Workflow:{0}:Raise WorkflowEvent:{1}".FormatTo(instance.Id, key), ex);
                }
            }
        }

        public static void RegisterWorkItemEvent(EventStep step, ItemActionType type, Action<IWorkflowInstance, IWorkflowItem> predicate)
        {
            if (predicate != null)
            {
                string key = KeyTemplate.FormatTo(step.ToString(), type.ToString());
                WorkItemEventList.Add(key, predicate);
            }
        }
    }
}
