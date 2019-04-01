using FoxOne._3VJ;
using FoxOne.Business;
using FoxOne.Business.Security;
using FoxOne.Core;
using FoxOne.Data;
using FoxOne.Workflow.Business;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using FoxOne.Controls;
using FoxOne.Workflow.Kernel;

namespace FoxOne.Web.Controllers
{

    /// <summary>
    /// OA移动端
    /// </summary>
    //[ActionViewUnTrance]
    public class MobileController:BaseController
    {

        private const int PAGE_SIZE = 20;

        /// <summary>
        /// 首页
        /// </summary>
        /// <returns></returns>
        [CustomUnAuthorize]
        public ActionResult Index()
        {
            var model = new IndexViewModel();
            int intT;
            try
            {
                model.MailCount = 0;//int.TryParse(new MailUserService().GetUnReadMail(Sec.User.Mail), out intT) ? intT : 0;
                model.NoticeCount = 0;
                model.ReadCount = WorkflowHelper.GetReadList(Sec.User.Id).Where(c => c.ItemStatus != WorkItemStatus.Finished.GetDescription()).Count();
                model.ToDoCount = WorkflowHelper.GetToDoList(Sec.User.Id).Count;
            }
            catch (Exception ex)
            {
                Logger.Error("Index", ex);
                model.MailCount = -1;
                model.NoticeCount = -1;
                model.ReadCount = -1;
                model.ToDoCount = -1;
            }
            return View(model);
        }

        /// <summary>
        /// 发起流程
        /// </summary>
        /// <param name="ApplicationId"></param>
        /// <returns></returns>
        public ActionResult StartWorkflow(int ApplicationId)
        {
            var workflowDetail = new WorkflowController().InitWorkflowHelper(Request);
            return View(workflowDetail);
        }

        private void RelatePage(WorkflowDetailVO workflowDetail)
        {
            List<Page> relPage = new List<Page>();
            workflowDetail.RelatePage.ForEach(c => {
                try
                {
                    var page = PageBuilder.BuildPage(c.Id + "_Mobile");
                    foreach (FoxOne.Business.IComponent com in page.Children)
                    {
                        if (com is NoPagerListControlBase)
                        {
                            var table = com as NoPagerListControlBase;
                            var ds = table.DataSource;
                            ds.Parameter = (c.Children.FirstOrDefault(o => o is NoPagerListControlBase) as NoPagerListControlBase).DataSource.Parameter;
                        }
                    }
                    relPage.Add(page);
                }
                catch (Exception ex)
                {

                }
            });
            workflowDetail.RelatePage = relPage;
        }

        /// <summary>
        /// 审批流程
        /// </summary>
        /// <returns></returns>
        public ActionResult WorkflowIndex()
        {
            var workflowDetail = new WorkflowController().InitWorkflowHelper(Request);
            //附加关联页面逻辑
            RelatePage(workflowDetail);
            return View(workflowDetail);
        }

        /// <summary>
        /// 查看流程详情
        /// </summary>
        /// <returns></returns>
        public ActionResult WorkflowIndexView()
        {
            var workflowDetail = new WorkflowController().InitWorkflowHelper(Request);
            //附加关联页面逻辑
            RelatePage(workflowDetail);
            return View(workflowDetail);
        }

        /// <summary>
        /// 选步骤选人界面
        /// </summary>
        /// <returns></returns>
        public ActionResult NextStep()
        {
            var runParameter = new WorkflowParameter();
            runParameter.InstanceId = Request["InstanceId"];
            var itemId = Request["ItemId"];
            int t;
            if (itemId == null || !int.TryParse(itemId.ToString(), out t))
            {
                throw new Exception("缺少工作项编号");
            }
            runParameter.ItemId = t;
            WorkflowHelper helper = new WorkflowHelper(Sec.User);
            helper.OpenWorkflow(runParameter.InstanceId, runParameter.ItemId);
            return View(new WorkflowController().GetNextStepInner(helper, runParameter.OpinionContent));
        }
        
        /// <summary>
        /// 意见页面
        /// </summary>
        /// <returns></returns>
        public ActionResult Opinion()
        {
            return View();
        }

        /// <summary>
        /// 流程成功页面
        /// </summary>
        /// <returns></returns>
        public ActionResult WorkflowSuccess()
        {
            return View();
        }

        /// <summary>
        /// 待办列表
        /// </summary>
        /// <returns></returns>
        public ActionResult ToDoList()
        {
            var result = WorkflowHelper.GetToDoList(Sec.User.Id).OrderByDescending(c=>c.ReceiveTime).Take(PAGE_SIZE).ToList();
            return View(FormatInstanceName(result));
        }

        /// <summary>
        /// 已办列表
        /// </summary>
        /// <returns></returns>
        public ActionResult DoneList()
        {
            var result = WorkflowHelper.GetDoneList(Sec.User.Id).OrderByDescending(c=>c.ReceiveTime).Take(PAGE_SIZE).ToList();
            return View(FormatInstanceName(result));
        }

        /// <summary>
        /// 知会列表
        /// </summary>
        /// <returns></returns>
        public ActionResult ReadList()
        {
            var result = WorkflowHelper.GetReadList(Sec.User.Id).Take(PAGE_SIZE).ToList();
            return View(FormatInstanceName(result));
        }

        /// <summary>
        /// 会议室预订一览
        /// </summary>
        /// <returns></returns>
        public ActionResult MeetingRoomBookPerView()
        {
            var meetingroomList = DBContext<DataDictionary>.Instance.FirstOrDefault(o => o.Code.Equals("MeetingRoom", StringComparison.OrdinalIgnoreCase)).Items;
            var meetingroomBook = Dao.Get().Query<MeetingRoomBookEntity>().ToList();
            var i =Request["tp"]==null? DateTime.Now:Request["tp"].ConvertTo<DateTime>();
            var bookInfo = meetingroomBook.Where(o => (TimeInRange(i, o.BeginTime) || TimeInRange(i, o.EndTime)));
            var info = meetingroomList.Where(c=>c.Status.Equals(DefaultStatus.Enabled.ToString(),StringComparison.CurrentCultureIgnoreCase)).Select(o => new MeetingRoomBookInfoView()
            {
                BookInfoDetail = bookInfo.Where(j => j.MeetingRoomId == o.Code).OrderBy(j => j.BeginTime).Select(k => new BookDetailView { BookId = k.Id, UserAndTime = DBContext<IUser>.Instance.Get(k.BookUserId).Name + "（" + k.BeginTime.ToString("HH: mm") + "至" + k.EndTime.ToString("HH: mm") + "）", Title = k.Title }).ToList(),
                Name = o.Name,
                Code = o.Code,
                BookInfo = bookInfo.Where(j => j.MeetingRoomId == o.Code).OrderBy(j => j.BeginTime).Select(k => DBContext<IUser>.Instance.Get(k.BookUserId).Name + "（" + k.BeginTime.ToString("HH: mm") + "至" + k.EndTime.ToString("HH: mm") + "）").ToList(),
                TimePoint = i,
            }).ToList();
            return View(info);
        }

        /// <summary>
        /// 新增预订
        /// </summary>
        /// <returns></returns>
        public ActionResult MeetingRoomBook(string MeetingRoomId)
        {
            //MeetingRoomBookEntity ent = new MeetingRoomBookEntity() { CreateTime = DateTime.Now, BookUserId = Sec.User.Id };
            //ent.MeetingRoomId = Request["RoomId"];
            //if (!bookId.IsNullOrEmpty())
            //{
            //    ent = Dao.Get().Query<MeetingRoomBookEntity>().FirstOrDefault(c => c.Id == bookId);
            //    if (ent == null)
            //    {
            //        throw new Exception("无法找到对应的会议室预定信息");
            //    }
            //}
            var page = PageBuilder.BuildPage("meetingroombookEdit");
            var form = page.Controls.FirstOrDefault(c => c is Form) as Form;
            form.PostUrl = "/Mobile/MeetingRoomBook";
            form.AppendQueryString = true;
            if (MeetingRoomId.IsNotNullOrEmpty())
            {
                form.FormMode = FormMode.Insert;
            }
            else
            {
                form.Key = Request[NamingCenter.PARAM_KEY_NAME];
                form.FormMode = FormMode.View;
            }
            
            return View(form);
        }


        /// <summary>
        /// 新增预订
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public JsonResult MeetingRoomBook(MeetingRoomBookEntity entity)
        {
            entity.BookUserId = Sec.User.Id;
            entity.CreateTime = DateTime.Now;
            entity.Id = Utility.GetGuid();
            var result = new MeetingRoomBookDataSource() { CRUDName = "mrmeetingroombook" }.Insert(entity.ToDictionary());
            return Json(result > 0);
        }

        /// <summary>
        /// 通知公告内容
        /// </summary>
        /// <returns></returns>
        public ActionResult NoticeList()
        {
            return View();
        }

        /// <summary>
        /// 通知公告详情
        /// </summary>
        /// <returns></returns>
        public ActionResult NoticeDetail(string id)
        {
            var detail = new CRUDDataSource() { CRUDName = "oanotice" }.Get(id);
            ViewData["Title"] = detail["Title"];
            ViewData["Content"] = detail["Content"];
            ViewData["CreateTime"] = detail["CreateTime"];
            return View();
        }

        /// <summary>
        /// 年会首页
        /// </summary>
        /// <returns></returns>
        public ActionResult PartyIndex()
        {
            bool isSign = Sec.User.Properties.ContainsKey("_ExtField_InParty");
            string disable = "disabled=\"disabled\"";
            return View(new PartyViewModel() {
                BtnSendEnable = isSign ? string.Empty : disable,
                BtnSignEnable = isSign ? disable : string.Empty,
            });
        }

        /// <summary>
        /// 年会签到成功
        /// </summary>
        /// <returns></returns>
        public ActionResult PartySignIn()
        {
            return View();
        }

        /// <summary>
        /// 年会发弹幕
        /// </summary>
        /// <returns></returns>
        public ActionResult PartySendMessage()
        {
            return View();
        }

        ///// <summary>
        ///// 知识库空间列表
        ///// </summary>
        ///// <returns></returns>
        //public ActionResult WikiAreaList()
        //{
        //    var ds = new WikiCRUDDataSource() {  CRUDName="wikizone"};
        //    var list = new List<WikiZone>();
        //    var datas = ds.GetList();
        //    if (!datas.IsNullOrEmpty())
        //    {
        //        datas.ForEach(c => {
        //            list.Add(c.ToEntity<WikiZone>());
        //        });
        //    }
        //    return View(list);
        //}

        ///// <summary>
        ///// 知识库目录列表
        ///// </summary>
        ///// <returns></returns>
        //public ActionResult WikiDir(string zoneId,string dirId,string keyword)
        //{
        //    var model = new WikiZoneContentViewModel();
        //    var zoneDs = new WikiCRUDDataSource() { CRUDName = "wikizone" };
        //    zoneDs.Parameter = new FoxOneDictionary<string, object>() { {"Id",zoneId } };
        //    var zone = zoneDs.GetList().FirstOrDefault();
        //    model.ZoneId = zone["Id"].ToString();
        //    var dirDs =new WikiCRUDDataSource(){ CRUDName= "wikidirectory" };
        //    dirDs.Parameter = new FoxOneDictionary<string, object>() {
        //         {"ZoneId",zoneId },
        //        //{ "ParentId",dirId},
        //    };
        //    var dirDatas = dirDs.GetList();
        //    model.DirList = dirDatas.Where(c => (dirId.IsNullOrEmpty() && (c["ParentId"] == null||c["ParentId"].ToString().IsNullOrEmpty())) || (dirId.IsNotNullOrEmpty() && dirId.Equals(c["ParentId"])));
        //    var docDs = new WikiDocFullTextDataSource() { CRUDName="wikidocument" };
        //    docDs.Parameter = new FoxOneDictionary<string, object>() {
        //         {"ZoneId",zoneId },
        //        { "ParentId",dirId.IsNullOrEmpty()?"Null": dirId},
        //        {"Keyword",keyword.IsNullOrEmpty()?"":keyword }
        //    };
        //    model.PathList = new List<Dictionary<string, string>>();
        //    var currParentId = dirId;
        //    while (currParentId.IsNotNullOrEmpty())
        //    {
        //        var parent = dirDatas.FirstOrDefault(c => currParentId.Equals(c["Id"]));
        //        if (!parent.IsNullOrEmpty())
        //        {
        //            model.PathList.Add(new Dictionary<string, string>() {

        //                {"Name",parent["Title"].ToString() },
        //                { "Id",parent["Id"].ToString()}
        //            });
        //            currParentId = parent["ParentId"].ToString();
        //        }
        //        else
        //        {
        //            currParentId = string.Empty;
        //        }
        //    }
        //    model.PathList.Add(new Dictionary<string, string>()
        //    {
        //        {"Name",zone["Name"].ToString() },
        //        { "Id",string.Empty}
        //    });
        //    model.PathList.Add(new Dictionary<string, string>()
        //    {
        //        {"Name","知识库" },
        //        { "Id","Area"}
        //    });
        //    if (!dirId.IsNullOrEmpty()) {
        //        model.CurrentDir = dirDatas.FirstOrDefault(c => c["Id"].ToString().Equals(dirId));
        //    }
        //    else
        //    {
        //        model.CurrentDir = zone;
        //        model.CurrentDir["btnDelete"] = zone["btnZoneDelete"];
        //        model.CurrentDir["btnEdit"] = zone["btnZoneEdit"];
        //        model.CurrentDir["Title"] = zone["Name"];
        //    }
        //    model.PathList.Reverse();
        //    model.DocList = docDs.GetList();
        //    return View(model);
        //}

        ///// <summary>
        ///// 知识库文章内容
        ///// </summary>
        ///// <returns></returns>
        //public ActionResult DocContent(string docId)
        //{
        //    var result = new WikiDocumentViewModel();
        //    var docDs = new WikiCRUDDataSource() { CRUDName = "wikidocument" };
        //    docDs.Parameter = new FoxOneDictionary<string, object>() {
        //        { "Id",docId}
        //    };
        //    result.Document = docDs.GetList().FirstOrDefault();
        //    var attDs = new WikiAttachmentDataSource() { OnlyShowAvaliable=true };
        //    attDs.Parameter = new FoxOneDictionary<string, object>() {
        //        {"DocId", docId},
        //    };
        //    result.AttachmentList= attDs.GetList();
        //    return View(result);
        //}

        public ActionResult WorkflowData(int pageIndex, string type,string key="")
        {
            IList<ToDoList> result=new List<ToDoList>();
            switch (type)
            {
                case "ToDo":
                    result = WorkflowHelper.GetToDoList(Sec.User.Id).Where(c=>c.InstanceName.Contains(key) || (c.Description.IsNotNullOrEmpty() && c.Description.Contains(key))).OrderByDescending(c => c.ReceiveTime).ToList();
                    break;
                case "Read":
                    result = WorkflowHelper.GetReadList(Sec.User.Id).Where(c => c.InstanceName.Contains(key) ||(c.Description.IsNotNullOrEmpty()&& c.Description.Contains(key))).ToList();
                    break;
                case "Done":
                    result = WorkflowHelper.GetDoneList(Sec.User.Id).Where(c => c.InstanceName.Contains(key) || (c.Description.IsNotNullOrEmpty() && c.Description.Contains(key))).OrderByDescending(c => c.ReceiveTime).ToList();
                    break;
                default:
                    break;
            }
            return Json(FormatInstanceName(result.Skip(pageIndex * PAGE_SIZE).Take(PAGE_SIZE).ToList()));
        }

        private bool TimeInRange(DateTime i, DateTime bookTime)
        {
            var rangeStart = new DateTime(i.Year, i.Month, i.Day, 6, 0, 0);
            var rangeEnd = new DateTime(i.Year, i.Month, i.Day, 23, 59, 0);
            return bookTime > rangeStart && bookTime < rangeEnd;
        }

        private IList<ToDoList> FormatInstanceName(IList<ToDoList> todoList)
        {
            todoList.ForEach(row => { row.InstanceName = row.InstanceName.Length > 15 ? row.InstanceName.Substring(0, 15) + "..." : row.InstanceName; });
            return todoList;
        }
    }

    public class IndexViewModel
    {
        /// <summary>
        /// 邮件数量
        /// </summary>
        public int MailCount { get; set; }

        /// <summary>
        /// 待办数量
        /// </summary>
        public int ToDoCount { get; set; }

        /// <summary>
        /// 通知公告数量
        /// </summary>
        public int NoticeCount { get; set; }

        /// <summary>
        /// 知会数量
        /// </summary>
        public int ReadCount { get; set; }

    }

    public class MeetingRoomBookInfoView
    {
        public string Name { get; set; }

        public string Code { get; set; }

        public IList<string> BookInfo { get; set; }

        public IList<BookDetailView> BookInfoDetail { get; set; }

        public DateTime TimePoint { get; set; }
    }

    public class BookDetailView
    {
        public string BookId { get; set; }

        public string Title { get; set; }

        public string UserAndTime { get; set; }


    }

    public class PartyViewModel
    {
        public string BtnSendEnable { get; set; }

        public string BtnSignEnable { get; set; }
    }

    public class WikiZoneContentViewModel
    {

        public string ZoneId { get; set; }

        public IDictionary<string,object> CurrentDir { get; set; }

        public IEnumerable<IDictionary<string,object>> DirList { get; set; }

        public IEnumerable<IDictionary<string,object>> DocList { get; set; }

        public List<Dictionary<string,string>> PathList { get; set; }
    }

    public class WikiDocumentViewModel
    {
        public IDictionary<string,object> Document { get; set; }

        public IEnumerable<IDictionary<string,object>> AttachmentList { get; set; }
    }
}