using FoxOne.Business;
using FoxOne.Business.Environment;
using FoxOne.Business.Security;
using FoxOne.Controls;
using FoxOne.Core;
using FoxOne.Data;
using FoxOne.Workflow.Business;
using FoxOne.Workflow.Kernel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Transactions;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using FoxOne.Web;
using System.Web.Script.Serialization;

namespace FoxOne.Web.Controllers
{
    /// <summary>
    /// 
    /// </summary>
    public class WorkflowController : BaseController
    {

        private const string APP_ID = "ApplicationId";
        private const string INST_ID = "InstanceId";
        private const string TASK_ID = "ItemId";
        private const string DATA_LOCATOR = "DataLocator";
        private const string AUTO_SEND = "自动发送";

        /// <summary>
        /// 流程新增或办理页
        /// </summary>
        /// <returns></returns>
        public ActionResult Index()
        {
            var detailVO = InitWorkflowHelper(Request);
            return View(detailVO);
        }

        /// <summary>
        /// 流程批量办理页面
        /// </summary>
        /// <param name="id">流程应用ID</param>
        /// <returns></returns>
        public ActionResult Batch(string id)
        {
            var app = DBContext<IWorkflowApplication>.Instance.FirstOrDefault(o => o.Id == id);
            string pageId = string.Empty;
            if (app == null)
            {
                throw new FoxOneException("不存编号为{0}的流程应用", id);
            }
            pageId = app.DocUrl;
            if (pageId.IsNullOrEmpty())
            {
                throw new FoxOneException("此流程应用未开启批量办理通道");
            }
            var page = PageBuilder.BuildPage(pageId);
            if (page == null)
            {
                throw new FoxOneException("页面不存在");
            }
            var table = page.Children.FirstOrDefault(o => o is Table) as Table;
            if (table == null)
            {
                throw new FoxOneException("请为页面{0}配置一个Table", id);
            }
            var ds = table.DataSource as BatchRunDataSource;
            if (ds == null)
            {
                throw new FoxOneException("批量审批列表页的数据源必须为【批量审批数据源】");
            }
            ds.ApplicationId = id;
            table.AllowPaging = false;
            ViewData["Table"] = table;
            return View();
        }

        /// <summary>
        /// 初始化流程实例
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public WorkflowDetailVO InitWorkflowHelper(HttpRequestBase request)
        {
            var appCode = request.QueryString[APP_ID];
            var dataLocator = request.QueryString[DATA_LOCATOR];
            var detailVO = new WorkflowDetailVO();
            detailVO.IsSimulate = "0";
            WorkflowHelper helper = null;
            IWorkflowApplication app = null;
            Form form = null;
            Page page = null;
            string formType = string.Empty;
            if (appCode.IsNotNullOrEmpty())
            {
                app = WorkflowHelper.GetApplication(appCode);
                formType = app.FormType;
                page = PageBuilder.BuildPage(app.FormType);
                form = page.Controls.FirstOrDefault(o => o is Form) as Form;
                form.FormMode = FormMode.Insert;
                detailVO.Title = app.Name;
                detailVO.ToolbarButtons = new List<WorkflowButtonType>() { WorkflowButtonType.Save };
            }
            else
            {
                if (dataLocator.IsNotNullOrEmpty())
                {
                    helper = new WorkflowHelper(Sec.User);
                    helper.OpenWorkflow(dataLocator);
                    formType = helper.FlowInstance.Application.FormType;
                    app = helper.FlowInstance.Application;
                    page = PageBuilder.BuildPage(formType);
                    form = page.Controls.FirstOrDefault(o => o is Form) as Form;
                    form.FormMode = FormMode.View;
                    detailVO.ToolbarButtons = new List<WorkflowButtonType>() { WorkflowButtonType.Rollback, WorkflowButtonType.SendOtherToRead };
                    detailVO.Title = helper.FlowInstance.InstanceName;
                    detailVO.RelatePage = GetRelateItem(formType, dataLocator, false);
                }
                else
                {
                    var instanceId = request.QueryString[INST_ID];
                    int itemId = request.QueryString[TASK_ID].ConvertTo<int>();
                    helper = new WorkflowHelper(Sec.User);
                    helper.OpenWorkflow(instanceId, itemId);
                    formType = helper.FlowInstance.Application.FormType;
                    app = helper.FlowInstance.Application;
                    page = PageBuilder.BuildPage(formType);
                    form = page.Controls.FirstOrDefault(o => o is Form) as Form;
                    if (helper.CurrentItem.Status >= WorkItemStatus.Finished || helper.CurrentItem.Alias == "传阅")
                    {
                        form.FormMode = FormMode.View;
                        detailVO.ToolbarButtons = new List<WorkflowButtonType>() { WorkflowButtonType.SendOtherToRead };
                        if (helper.CurrentItem.Alias != "传阅")
                        {
                            detailVO.ToolbarButtons.Add(WorkflowButtonType.Rollback);
                        }
                        detailVO.Title = string.Format("{0}", helper.FlowInstance.InstanceName);
                        detailVO.RelatePage = GetRelateItem(formType, helper.FlowInstance.DataLocator, false);
                    }
                    else
                    {
                        detailVO.CanDeleteInstance = helper.CurrentItem.PartUserId.Equals(helper.FlowInstance.CreatorId, StringComparison.OrdinalIgnoreCase);
                        detailVO.CanUploadFile = true;
                        detailVO.ShowOpinionInput = (helper.CurrentItem.ItemId != 1);
                        if (!helper.CurrentItem.PartUserId.Equals(Sec.User.Id, StringComparison.OrdinalIgnoreCase) && !Sec.IsSuperAdmin)
                        {
                            throw new FoxOneException("您没有处理该审批步骤的权限！");
                        }
                        form.FormMode = FormMode.Edit;
                        ProcessActivityPermission(form, helper);
                        detailVO.ToolbarButtons = new List<WorkflowButtonType>() { WorkflowButtonType.Save, WorkflowButtonType.Send, WorkflowButtonType.SendOtherToRead };
                        if (detailVO.CanDeleteInstance)
                        {
                            detailVO.ToolbarButtons.Add(WorkflowButtonType.Delete);
                            detailVO.RelatePage = GetRelateItem(formType, helper.FlowInstance.DataLocator, true);
                        }
                        else
                        {
                            detailVO.ToolbarButtons.Add(WorkflowButtonType.BackToRoot);
                            detailVO.ToolbarButtons.Add(WorkflowButtonType.ForceEnd);
                            detailVO.ToolbarButtons.Add(WorkflowButtonType.Pushback);
                            detailVO.RelatePage = GetRelateItem(formType, helper.FlowInstance.DataLocator, false);
                        }
                        detailVO.Title = string.Format("{0}-{1}", helper.FlowInstance.InstanceName, helper.CurrentItem.Alias);
                    }
                }
                helper.SetReadTime();
                form.Key = helper.FlowInstance.DataLocator;
                detailVO.WorkItem = GetWorkItem(helper);
                detailVO.NoticeItem = GetNoticeItem(helper);
                if (app.NeedAttachement)
                {
                    detailVO.Attachment = GetAttachmentInfo(app.Id ,form.Key, detailVO.CanUploadFile);
                    detailVO.ShowAttachment = !detailVO.Attachment.IsNullOrEmpty();
                }
                detailVO.ShowWorkItem = true;
                detailVO.InstanceId = helper.FlowInstance.Id;
                detailVO.ItemId = helper.CurrentItem.ItemId;
                if (!SysConfig.IsProductEnv && request.QueryString["IsSimulate"].IsNotNullOrEmpty())
                {
                    detailVO.IsSimulate = request.QueryString["IsSimulate"];
                }
            }
            form.AutoHeight = false;
            form.AppendQueryString = true;
            form.PostUrl = "/Workflow/Save";
            detailVO.Form = form;
            detailVO.DefinitionId = app.WorkflowId;
            return detailVO;
        }

        /// <summary>
        /// 处理步骤权限
        /// </summary>
        /// <param name="form"></param>
        /// <param name="helper"></param>
        private void ProcessActivityPermission(Form form, WorkflowHelper helper)
        {
            var activityPermission = Dao.Get().Query<WorkflowActivityPermission>().Where(o => o.ApplicationId == helper.FlowInstance.ApplicationId).OrderBy(o => o.Priority).ToList();
            if (!activityPermission.IsNullOrEmpty())
            {
                //正序执行，优先级越高的越后执行，可以覆盖前面的设置。
                foreach (var item in activityPermission)
                {
                    if (item.UserIds.IsNotNullOrEmpty() && !item.UserIds.Split(',').Contains(Sec.User.Id, StringComparer.OrdinalIgnoreCase))
                    {
                        continue;
                    }
                    if (item.ActivityName.IsNotNullOrEmpty() && !item.ActivityName.Split(',').Contains(helper.CurrentActivity.Alias, StringComparer.OrdinalIgnoreCase))
                    {
                        continue;
                    }
                    if (item.ControlIds.IsNullOrEmpty())
                    {
                        continue;
                    }
                    foreach (var ctrl in item.ControlIds.Split(','))
                    {
                        var field = form.Fields.FirstOrDefault(o => o.Id.Equals(ctrl, StringComparison.OrdinalIgnoreCase));
                        if (field != null)
                        {
                            switch (item.Behaviour)
                            {
                                case ControlSecurityBehaviour.Enabled:
                                    field.Enable = true;
                                    break;
                                case ControlSecurityBehaviour.Disabled:
                                    field.Enable = false;
                                    break;
                                case ControlSecurityBehaviour.Visible:
                                    field.Visiable = true;
                                    break;
                                case ControlSecurityBehaviour.Invisible:
                                    field.Visiable = false;
                                    break;
                                case ControlSecurityBehaviour.Required:
                                    field.Validator = "required";
                                    break;
                                case ControlSecurityBehaviour.InRequired:
                                    field.Validator = "";
                                    break;
                                default:
                                    break;
                            }
                        }
                    }
                }
            }
        }


        /// <summary>
        /// 流程模拟时自动跳到下一步
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public ActionResult AutoRun(string id)
        {
            //if (SysConfig.IsProductEnv)
            //{
            //    throw new FoxOneException("不允许此操作");
            //}
            WorkflowHelper helper = new WorkflowHelper(Sec.User);
            helper.OpenWorkflow(id, 1);
            var unDoItem = helper.FlowInstance.WorkItems.Where(o => o.Status < WorkItemStatus.Finished);
            if (!unDoItem.IsNullOrEmpty())
            {
                var newItem = unDoItem.OrderBy(o => o.ItemId).First();
                var loginId = DBContext<IUser>.Instance.Get(newItem.PartUserId).LoginId;
                FormsAuthentication.SignOut();
                FormsAuthentication.SetAuthCookie(loginId, false);
                return Redirect("/Workflow/Index?InstanceId={0}&ItemId={1}&IsSimulate=1".FormatTo(newItem.InstanceId, newItem.ItemId));
            }
            else
            {
                var loginId = "liuhf";
                FormsAuthentication.SignOut();
                FormsAuthentication.SetAuthCookie(loginId, false);
                return Redirect("/Page/InstanceList");
            }
        }


        private IEnumerable<WorkItemVO> GetWorkItem(WorkflowHelper helper)
        {
            return helper.FlowInstance.WorkItems.OrderBy(o => o.ItemId).Select(o => new WorkItemVO()
            {
                ActivityName = o.Alias,
                ItemId = o.ItemId,
                CreatorUserId = o.PartUserId,
                Opinion = o.OpinionContent,
                ShowTime = o.ItemId == 1 ? (o.ReceiveTime.Value.ToString("申请时间：yyyy-MM-dd HH:mm")) : (o.FinishTime.HasValue ? o.FinishTime.Value.ToString("审批时间：yyyy-MM-dd HH:mm") : (o.ReadTime.HasValue ? o.ReadTime.Value.ToString("阅读时间：yyyy-MM-dd HH:mm") : o.ReceiveTime.Value.ToString("送达时间：yyyy-MM-dd HH:mm"))),
                Status = o.Status.ToString(),
                StatusText = o.ItemId == 1 ? "申请" : o.StatusText,
                Creator = o.PartUserName,
            });
        }

        private IEnumerable<AttachmentInfoVO> GetAttachmentInfo(string appId, string formKey, bool canUpload)
        {
            var list = Dao.Get().Query<AttachmentEntity>().Where(o => o.RelateId == formKey).ToList();
            try
            {
                bool isLocal = false;//到时候需要根据数据源类型判断
                var ds = DataSource(appId);
                List<AttachmentEntity> datas = new List<AttachmentEntity>();
                if (ds != null)
                {
                    InitParameter(ds,formKey);
                    datas.AddRange(ds.GetList().ToList().ToEntities<AttachmentEntity>());
                }
                else
                {
                    isLocal = true;
                    datas.AddRange(list);
                }
                return datas.Select(o => new AttachmentInfoVO()
                {
                    CreateTime = o.CreateTime.ToString("yyyy-MM-dd HH:mm"),
                    CreatorName = DBContext<IUser>.Instance.Count(u => u.Id == o.CreatorId) <= 0 ? o.CreatorId : DBContext<IUser>.Instance.FirstOrDefault(u => u.Id == o.CreatorId).Name,
                    FilePath = o.FilePath,
                    FileSize = (o.FileSize / 1024) + "KB",
                    Icon = o.FileType.Substring(1) + ".png",
                    FileName = o.FileName,
                    FileId = o.Id,
                    IsLocalResource = isLocal,
                    CanDelete = o.CreatorId.Equals(Sec.User.Id, StringComparison.OrdinalIgnoreCase) && canUpload
                });
            }
            catch (Exception ex)
            {
                Logger.Error("WorkflowController GetAttachmentInfo", ex);
                return null;
            }
        }

        private void InitParameter(ListDataSourceBase ds,string formKey)
        {
            var param = ds.Parameter;
            if (param == null)
            {
                param = new FoxOneDictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            }
            foreach (var key in System.Web.HttpContext.Current.Request.QueryString.AllKeys)
            {
                if (!System.Web.HttpContext.Current.Request.QueryString[key].IsNullOrEmpty())
                {
                    param[key] = HttpUtility.UrlDecode(System.Web.HttpContext.Current.Request.QueryString[key]);
                }
            }
            foreach (var key in System.Web.HttpContext.Current.Request.Form.AllKeys)
            {
                if (!System.Web.HttpContext.Current.Request.Form[key].IsNullOrEmpty())
                {
                    param[key] = HttpUtility.UrlDecode(System.Web.HttpContext.Current.Request.Form[key]);
                }
            }
            param["RelateId"] = formKey;
            ds.Parameter = param;
        }

        private ListDataSourceBase DataSource(string appId)
        {
            var compnent = DBContext<ComponentEntity>.Instance.FirstOrDefault(c => c.PageId == "ApplicationList" && c.ParentId == appId);
            ListDataSourceBase result = null;
            if (compnent != null)
            {
                JavaScriptSerializer serializer = new JavaScriptSerializer();
                serializer.RegisterConverters(new[] { new Business.ComponentConverter() });
                var ds = serializer.Deserialize(compnent.JsonContent, TypeHelper.GetType(compnent.Type));
                result = ds as ListDataSourceBase;
            }
            return result;
        }



        private IEnumerable<WorkItemVO> GetNoticeItem(WorkflowHelper helper)
        {
            return helper.FlowInstance.WorkItemsRead.OrderBy(o => o.ItemId).Select(o => new WorkItemVO()
            {
                ActivityName = o.Alias,
                ItemId = o.ItemId,
                CreatorUserId = o.PartUserId,
                Opinion = o.OpinionContent,
                ShowTime = o.FinishTime.HasValue ? o.FinishTime.Value.ToString("完成时间：yyyy-MM-dd HH:mm") : (o.ReadTime.HasValue ? o.ReadTime.Value.ToString("阅读时间：yyyy-MM-dd HH:mm") : o.ReceiveTime.Value.ToString("接收时间：yyyy-MM-dd HH:mm")),
                Status = o.Status.ToString(),
                StatusText = o.StatusText == "送达" ? "未读" : "已读",
                Creator = o.PartUserName
            });
        }

        private IEnumerable<Page> GetRelateItem(string formType, string formKey, bool canModify)
        {
            var tempRelate = DBContext<PageRelateEntity>.Instance.Where(o => o.PageId.Equals(formType, StringComparison.OrdinalIgnoreCase));
            var result = new List<Page>();
            string pageId = string.Empty;
            string fkName = string.Empty;
            foreach (var item in tempRelate)
            {
                string[] temp = item.RelateUrl.Split(',');
                pageId = temp[0];
                fkName = temp[1];
                Page page = PageBuilder.BuildPage(pageId);
                foreach (FoxOne.Business.IComponent com in page.Children)
                {
                    if (com is Toolbar)
                    {
                        var toolbar = com as Toolbar;
                        var modifyBtns = toolbar.Buttons.Where(o => o.Id.Equals("btnInsert") || o.Id.Equals("btnBatchDelete"));
                        modifyBtns.ForEach(o => o.Visiable = canModify);
                    }
                    if (com is Search)
                    {
                        var search = com as Search;
                        var modifyBtns = search.Buttons.Where(o => o.Id.Equals("btnInsert") || o.Id.Equals("btnBatchDelete"));
                        modifyBtns.ForEach(o => o.Visiable = canModify);
                    }
                    if (com is Table)
                    {
                        var table = com as Table;
                        var ds = table.DataSource;
                        if (ds.Parameter == null)
                        {
                            ds.Parameter = new FoxOneDictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                        }
                        ds.Parameter.Add(fkName, formKey);
                        table.InsertUrl += "?{0}={1}".FormatTo(fkName, formKey);
                        var modifyBtns = table.Buttons.Where(o => o.Id.Equals("btnEdit") || o.Id.Equals("btnDelete"));
                        modifyBtns.ForEach(o => o.Visiable = canModify);
                    }
                }
                result.Add(page);
            }
            return result;
        }

        /// <summary>
        /// 保存表单
        /// </summary>
        /// <returns></returns>
        [ValidateInput(false)]
        public JsonResult Save()
        {
            IDictionary<string, object> data = Request.Form.ToDictionary();
            string key = string.Empty;
            string pageId = Request.QueryString[NamingCenter.PARAM_PAGE_ID];
            string ctrlId = Request.QueryString[NamingCenter.PARAM_CTRL_ID];
            string formViewMode = Request.Form[NamingCenter.PARAM_FORM_VIEW_MODE];
            var page = PageBuilder.BuildPage(pageId);
            if (page == null)
            {
                throw new FoxOneException("Page_Not_Found");
            }
            var form = page.FindControl(ctrlId) as Form;
            if (form == null)
            {
                throw new FoxOneException("Ctrl_Not_Found");
            }
            var ds = form.FormService as IFormService;
            int effectCount = 0;
            WorkflowHelper helper = new WorkflowHelper(Sec.User);
            if (formViewMode.Equals(FormMode.Edit.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                string instanceId = Request.Form[INST_ID];
                int itemId = Request.Form[TASK_ID].ConvertTo<int>();
                helper.OpenWorkflow(instanceId, itemId);
                key = helper.FlowInstance.DataLocator;
                effectCount = ds.Update(key, data);
                //helper.UpdateInstance(Env.Parse(helper.FlowInstance.Application.InstanceTitleTemplate), key, 0, 0);
                helper.UpdateInstance(helper.FlowInstance.InstanceName, key, 0, 0);
                return Json(new { Insert = false, InstanceId = instanceId, ItemId = itemId, DataLocator = key, ApplicationId = helper.FlowInstance.ApplicationId });
            }
            else
            {
                string applicationId = Request.Form[APP_ID];
                key = Utility.GetGuid();
                string keyField = form.Key.IsNullOrEmpty() ? "Id" : form.Key;
                data[keyField] = key;
                effectCount = ds.Insert(data);
                IWorkflowApplication app = DBContext<IWorkflowApplication>.Instance.Get(applicationId);
                helper.StartWorkflow(applicationId, Env.Parse(app.InstanceTitleTemplate), key, 0, 0);
                return Json(new { Insert = true, InstanceId = helper.FlowInstance.Id, ItemId = 1, DataLocator = key, ApplicationId = applicationId });
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public JsonResult GetToDoList()
        {
            return Json(WorkflowHelper.GetToDoList(Sec.User.Id), JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// 发起流程
        /// </summary>
        /// <param name="startParameter"></param>
        /// <returns></returns>
        public JsonResult Start(WorkflowStartParameter startParameter)
        {
            WorkflowHelper helper = new WorkflowHelper(Sec.User);
            helper.StartWorkflow(startParameter.AppCode, startParameter.InstanceName, startParameter.DataLocator, startParameter.ImportLevel, startParameter.SecurityLevel);
            return Json(helper.FlowInstance.Id);
        }

        /// <summary>
        /// 删除流程实例
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public JsonResult Delete(string id)
        {
            if (id.IsNotNullOrEmpty() && Sec.IsSuperAdmin)
            {
                WorkflowHelper helper = new WorkflowHelper(Sec.User);
                helper.OpenWorkflow(id);
                DeleteWorkflow(helper);
                return Json(true);
            }
            return Json(false);
        }

        private void DeleteWorkflow(WorkflowHelper helper)
        {
            using (TransactionScope tran = new TransactionScope())
            {
                var service = GetForm(helper);
                service.Delete(helper.FlowInstance.DataLocator);
                Dao.Get().Delete<AttachmentEntity>().Where(o => o.RelateId == helper.FlowInstance.DataLocator).Execute();
                helper.DeleteWorkflow();
                tran.Complete();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="helper"></param>
        /// <returns></returns>
        public IFormService GetForm(WorkflowHelper helper)
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
                    helper.SetParameter(item.Key, item.Value == null ? null : item.Value.ToString());
                }
            }

            return result;
        }

        /// <summary>
        /// 批量审批
        /// </summary>
        /// <param name="id"></param>
        /// <param name="action"></param>
        /// <returns></returns>
        public JsonResult BatchRun(string id, string action)
        {
            if (id.IsNullOrEmpty())
            {
                throw new FoxOneException("Parameter_Not_Null", "id");
            }
            bool result = false;
            if (id.IndexOf(',') > 0)
            {
                int successCount = 0;
                string[] ids = id.Split(new char[] { ',', '|', ';' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var item in ids)
                {
                    if (BatchRunInner(item, action))
                    {
                        successCount++;
                    }
                }
                result = successCount > 0;
            }
            else
            {
                result = BatchRunInner(id, action);
            }
            return Json(result);
        }

        private bool BatchRunInner(string id, string action)
        {
            bool result = false;
            WorkflowHelper helper = new WorkflowHelper(Sec.User);
            helper.OpenWorkflow(id);
            if (helper.CurrentItem.PartUserId.IsNullOrEmpty() || !helper.CurrentItem.PartUserId.Equals(Sec.User.Id, StringComparison.OrdinalIgnoreCase))
            {
                throw new FoxOneException("您不能办理当前步骤！");
            }
            switch (action)
            {
                case "run":
                    var trans = GetNextStepInner(helper, "同意");
                    if (trans.Count == 1 && trans[0].StepName == AUTO_SEND)
                    {
                        result = true;
                    }
                    else
                    {
                        throw new FoxOneException("当前步骤需要选择，不允许快速办理");
                    }
                    break;
                case "forceend":
                    var form1 = GetForm(helper) as IFlowFormService;
                    if (form1 != null)
                    {
                        form1.OnFlowFinish(helper.FlowInstance.Id, helper.FlowInstance.DataLocator, false, "不同意");
                    }
                    helper.SetOpinion("不同意", 1);
                    result = helper.ForceToEnd();
                    break;
                case "pushback":
                    helper.SetOpinion("退回上一步", 1);
                    result = helper.Pushback();
                    break;
                case "backtoroot":
                    helper.SetOpinion("退回拟稿人", 1);
                    result = helper.PushbackToRoot();
                    break;
            }
            return result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="runParameter"></param>
        /// <returns></returns>
        public JsonResult GetNextStep(WorkflowParameter runParameter)
        {
            WorkflowHelper helper = new WorkflowHelper(Sec.User);
            helper.OpenWorkflow(runParameter.InstanceId, runParameter.ItemId);
            return Json(GetNextStepInner(helper, runParameter.OpinionContent));
        }

        internal List<NextStep> GetNextStepInner(WorkflowHelper helper, string opinionContent)
        {
            List<NextStep> trans = new List<NextStep>();

            var form = GetForm(helper) as IFlowFormService;
            if (form == null || form.CanRunFlow())
            {
                if (!helper.ShowUserSelect())
                {
                    if (!string.IsNullOrEmpty(opinionContent))
                    {
                        helper.SetOpinion(opinionContent, 1);
                    }
                    helper.CurrentWorkflow.OnActivityEnter += (IWorkflowContext context, IActivity activity) =>
                    {
                        if (activity is EndActivity)
                        {
                            if (form != null)
                            {
                                form.OnFlowFinish(context.FlowInstance.Id, context.FlowInstance.DataLocator, true, string.Empty);
                            }
                        }
                    };
                    helper.Run();
                    trans.Add(new NextStep() { StepName = AUTO_SEND, Label = AUTO_SEND });
                }
                else
                {
                    trans = helper.GetNextStep();
                    if (trans.Count == 1)
                    {
                        var step = trans[0];
                        if (step.Users.Count == 1 || step.NeedUser == false)
                        {
                            helper.CurrentWorkflow.OnActivityEnter += (IWorkflowContext context, IActivity activity) =>
                            {
                                if (activity is EndActivity)
                                {
                                    if (form != null)
                                    {
                                        form.OnFlowFinish(context.FlowInstance.Id, context.FlowInstance.DataLocator, true, string.Empty);
                                    }
                                }
                            };
                            if (!string.IsNullOrEmpty(opinionContent))
                            {
                                helper.SetOpinion(opinionContent, 1);
                            }
                            if (step.Users == null || step.Users.Count == 0)
                            {
                                helper.Run(helper.GetUserChoice("NULL_NULL_" + step.StepName));
                            }
                            else
                            {
                                helper.Run(helper.GetUserChoice(step.StepName, step.Users[0]));
                            }
                            trans[0].StepName = AUTO_SEND;
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

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public ActionResult FlowSuccess()
        {
            string message = string.Empty;
            var instanceId = Request.QueryString[INST_ID];
            int itemId = Request.QueryString[TASK_ID].ConvertTo<int>();
            var helper = new WorkflowHelper(Sec.User);
            try
            {
                helper.OpenWorkflow(instanceId, itemId);
                if (helper.FlowInstance.FlowTag == FlowStatus.Finished)
                {
                    message = $"流程【{helper.FlowInstance.InstanceName}】已结束审批";
                }
                else
                {
                    if (helper.CurrentItem.Status == WorkItemStatus.Finished)
                    {
                        var nextItem = helper.FlowInstance.WorkItems.Where(o => o.PreItemId == itemId);
                        string newActivity = string.Join("，", nextItem.Distinct(o => o.CurrentActivity).Select(o => o.Alias));
                        string userName = string.Join("，", nextItem.Distinct(o => o.PartUserName).Select(o => o.PartUserName));
                        message = $"流程【{helper.FlowInstance.InstanceName}】已流转至步骤【{newActivity}】的处理人【{userName}】处";
                    }
                    else
                    {
                        message = $"流程【{helper.FlowInstance.InstanceName}】已{helper.CurrentItem.StatusText}";
                    }
                }
            }
            catch (FoxOneException)
            {
                message = "流程实例删除成功";
            }

            ViewData["Message"] = message;
            return View();
        }

        /// <summary>
        /// 传阅
        /// </summary>
        /// <returns></returns>
        public JsonResult CC()
        {
            WorkflowHelper helper = new WorkflowHelper(Sec.User);
            var instanceId = Request[INST_ID];
            string partUserIds = Request["UserIds"];
            helper.OpenWorkflow(instanceId, 1);
            helper.SendToOtherToRead(partUserIds);
            return Json(true);
        }

        /// <summary>
        /// 执行流程流转动作
        /// </summary>
        /// <param name="runParameter"></param>
        /// <returns></returns>
        public JsonResult ExecCommand(WorkflowRunParameter runParameter)
        {
            WorkflowHelper helper = new WorkflowHelper(Sec.User);
            helper.OpenWorkflow(runParameter.InstanceId, runParameter.ItemId);
            if (!string.IsNullOrEmpty(runParameter.OpinionContent))
            {
                helper.SetOpinion(HttpUtility.HtmlEncode(runParameter.OpinionContent), runParameter.OpinionArea);
            }
            bool result = false;
            switch (runParameter.Command.ToLower())
            {
                case "run":
                    var form = GetForm(helper) as IFlowFormService;
                    if (form == null || form.CanRunFlow())
                    {
                        helper.CurrentWorkflow.OnActivityEnter += (IWorkflowContext context, IActivity activity) =>
                        {
                            Logger.Info("Raise OnActivityEnter：" + activity.Alias);
                            if (activity is EndActivity)
                            {
                                if (form != null)
                                {
                                    form.OnFlowFinish(context.FlowInstance.Id, context.FlowInstance.DataLocator, true, string.Empty);
                                }
                            }
                        };
                        helper.Run(runParameter.RunContext());
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
                    var form1 = GetForm(helper) as IFlowFormService;
                    if (form1 != null)
                    {
                        form1.OnFlowFinish(helper.FlowInstance.Id, helper.FlowInstance.DataLocator, false, runParameter.OpinionContent);
                    }
                    result = helper.ForceToEnd();
                    break;
                case "switch":
                    //helper.Switch(runParameter.RunContext.First());
                    result = true;
                    break;
                case "delete":
                    DeleteWorkflow(helper);
                    result = true;
                    break;
            }
            return Json(result);
        }
    }



    /// <summary>
    /// 工作流相关的数据源
    /// </summary>
    [DisplayName("工作流数据源")]
    public class WorkflowDataSource : ListDataSourceBase, IKeyValueDataSource, IFormService
    {
        /// <summary>
        /// 
        /// </summary>
        [DisplayName("数据源类型")]
        public WorkflowDataSourceType SourceType { get; set; }


        private IList<IDictionary<string, object>> Items { get; set; }

        private bool DataIsEmpty { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public IDictionary<string, object> FormData
        {
            get; set;
        }

        public object Converter(string columnName, object columnValue, IDictionary<string, object> rowData)
        {
            if (Items == null && !DataIsEmpty)
            {
                Items = GetListInner().ToList();
                DataIsEmpty = Items.IsNullOrEmpty();
            }
            if (Items == null)
            {
                return columnValue;
            }
            else
            {
                return Items.FirstOrDefault(o => o["Id"].ToString() == columnValue.ToString())["Name"];
            }
        }

        protected override IEnumerable<IDictionary<string, object>> GetListInner()
        {
            WorkflowHelper helper = new WorkflowHelper(Sec.User);
            IList<IDictionary<string, object>> result = null;
            switch (SourceType)
            {
                case WorkflowDataSourceType.ToDo:
                    result = WorkflowHelper.GetToDoList(Sec.User.Id).OrderByDescending(o => o.ReceiveTime).ToDictionary();
                    break;
                case WorkflowDataSourceType.Done:
                    result = WorkflowHelper.GetDoneList(Sec.User.Id).OrderByDescending(o => o.ReceiveTime).ToDictionary();
                    break;
                case WorkflowDataSourceType.Read:
                    result = WorkflowHelper.GetReadList(Sec.User.Id).OrderBy(c => c.FinishTime).ThenByDescending(o => o.ReceiveTime).ToDictionary();
                    break;
                case WorkflowDataSourceType.Definition:
                    result = WorkflowHelper.GetAllDefinition().ToDictionary();
                    break;
                case WorkflowDataSourceType.Application:
                    result = WorkflowHelper.GetAllApplication().ToDictionary();
                    break;
                case WorkflowDataSourceType.Instance:
                    result = WorkflowHelper.GetAllInstance().OrderByDescending(o => o.StartTime).ToDictionary();
                    break;
                default:
                    break;
            }
            return result;
        }

        public IEnumerable<TreeNode> SelectItems()
        {
            WorkflowHelper helper = new WorkflowHelper(Sec.User);
            IEnumerable<TreeNode> result = null;
            switch (SourceType)
            {
                case WorkflowDataSourceType.Definition:
                    result = WorkflowHelper.GetAllDefinition().Select(o => new TreeNode() { Text = o.Name, Value = o.Id });
                    break;
                case WorkflowDataSourceType.Application:
                    result = WorkflowHelper.GetAllApplication().Select(o => new TreeNode() { Text = o.Name, Value = o.Id });
                    break;
                default:
                    throw new FoxOneException("Not Support!");
            }
            return result;
        }

        public int Insert(IDictionary<string, object> data)
        {
            throw new NotImplementedException();
        }

        public int Update(string key, IDictionary<string, object> data)
        {
            throw new NotImplementedException();
        }

        public IDictionary<string, object> Get(string key)
        {
            throw new NotImplementedException();
        }

        public int Delete(string key)
        {
            WorkflowHelper helper = new WorkflowHelper(Sec.User);
            bool result = false;
            switch (SourceType)
            {
                case WorkflowDataSourceType.Definition:
                    if (DBContext<IWorkflowApplication>.Instance.Count(c => c.WorkflowId == key) > 0)
                    {
                        throw new FoxOneException("当前流程定义有关联的流程应用，不能删除！");
                    }
                    var components = DBContext<ComponentEntity>.Instance.Where(i => i.PageId.Equals(key, StringComparison.OrdinalIgnoreCase));
                    if (!components.IsNullOrEmpty())
                    {
                        components.ForEach(k =>
                        {
                            DBContext<ComponentEntity>.Delete(k);
                        });
                    }
                    result = DBContext<IWorkflowDefinition>.Delete(key);
                    break;
                case WorkflowDataSourceType.Application:
                    if (DBContext<IWorkflowInstance>.Instance.Count(c => c.ApplicationId == key) > 0)
                    {
                        throw new FoxOneException("当前流程应用有关联的实例，不能删除！");
                    }
                    result = DBContext<IWorkflowApplication>.Delete(key);
                    break;
                default:
                    throw new FoxOneException("Not Support!");
            }
            return result ? 1 : 0;
        }
    }














}
