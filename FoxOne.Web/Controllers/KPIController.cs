using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using System.Web.Security;
using FoxOne.Data;
using FoxOne.Controls;
using FoxOne.Business;
using FoxOne.Core;
using FoxOne.Business.Security;
using FoxOne.Business.OAuth;
using FoxOne.Business.Environment;
using FoxOne.Workflow.Business;
using FoxOne._3VJ;
using FoxOne.Data.Attributes;
using System.ComponentModel;
using System.Transactions;
using System.IO;
using System.Text;

namespace FoxOne.Web.Controllers
{
    public class KPIController : BaseController
    {
        private const string FORM_KEY = "_FORM_KEY";
        private const string KPI_KEY = "KpiId";
        private const string KPI_APPROVE_STATUS = "KpiStatus";

        private readonly static string[] typeArr = new string[] { "Target", "Score", "Case" };
        private readonly static Dictionary<string,string> gpArr = new Dictionary<string, string> { { "Stable","稳定性" }, { "Business", "业务" }, {  "Manage","管理" }, {  "Study","个人成长" }, { "Summary" , "总结"} };

        public ActionResult Index()
        {
            var id = Request[FORM_KEY];
            var vm = new KpiViewModel();
            var vModelList = new List<KpiDetailViewModel>();
            if (id.IsNotNullOrEmpty())//库中有记录
            {
                vm.KPIEntity = Dao.Get().Query<KPIEntity>().FirstOrDefault(o => o.Id == id);
                vm.IsNew = false;
                vModelList.AddRange(InitDetailView(vm.KPIEntity.Status));
                var dic = vModelList.ToDictionary(c => c.Key);

                var vDatas = Dao.Get().Query<KPIDetailEntity>().Where(c => c.KPIId == id).ToList();
                //Stable-Target,Stable-Score,Business-Target,Business-Score 分组后  Stable:Stable-Target,Stable-Score;Business:Business-Target,Business-Score
                vDatas.Where(c => c.Key.Contains('-')).GroupBy(c => c.Key.Substring(0, c.Key.IndexOf('-'))).ForEach(c => {
                    c.ForEach(i => dic[c.Key].DetailValues[i.Key] = i.Value);
                });
            }
            else
            {
                //查找出上级主管
                //var role = Sec.User.Department.Roles.FirstOrDefault(c => c.RoleType.Name.Equals("经理") || c.RoleType.Name.Equals("总监"));
                var role = GetLeader(Sec.User.Department);
                vm.KPIEntity = new KPIEntity();
                if (role != null && !role.Members.IsNullOrEmpty())
                {
                    vm.KPIEntity.Approvaler = role.Members.FirstOrDefault().Id;
                }
                vm.KPIEntity.KPIMonth = DateTime.Now.AddMonths(-1);
                vm.KPIEntity.CreatorId = Sec.User.Id;
                vm.IsNew = true;
                ///初始化KpiDetail
                vModelList.AddRange(InitDetailView());
            }
            vm.SelfEditable = vm.KPIEntity.Status == KpiStatus.Init;
            vm.ApproveEditable = vm.KPIEntity.Status == KpiStatus.Submit && Sec.User.Id.Equals(vm.KPIEntity.Approvaler);
            if (vm.KPIEntity.Approvaler.IsNotNullOrEmpty())
            {
                vm.ApprovorName = DBContext<IUser>.Instance.FirstOrDefault(c => c.Id.Equals(vm.KPIEntity.Approvaler)).Name;
            }
            if ((vm.KPIEntity.Status == KpiStatus.Init && Sec.User.Id.Equals(vm.KPIEntity.CreatorId)) || //创建者进入且在未提交状态
                vm.KPIEntity.Status == KpiStatus.Submit && Sec.User.Id.Equals(vm.KPIEntity.Approvaler))//审核人进入且未审核状态
            {
                vm.Optionable = true;
            }
            vm.CreatorName= DBContext<IUser>.Instance.FirstOrDefault(c => c.Id.Equals(vm.KPIEntity.CreatorId)).Name;
            vm.DetailList = vModelList;
            return View(vm);
        }

        private IRole GetLeader(IDepartment department)
        {
            if (Sec.User.Department == department && Sec.User.Roles.Count(c => c.RoleType.Name.Equals("经理") || c.RoleType.Name.Equals("总监")) > 0)
            {
                if (department.Parent != null)
                {
                    return GetLeader(department.Parent);
                }
                return department.Roles.FirstOrDefault();
            }
            return department.Roles.FirstOrDefault(c => c.RoleType.Name.Equals("经理") || c.RoleType.Name.Equals("总监"));
        }

        private IList<KpiDetailViewModel> InitDetailView(KpiStatus status= KpiStatus.Init)
        {
            var list = new List<KpiDetailViewModel>();            
            foreach (var gp in gpArr)
            {
                var detail = new KpiDetailViewModel() { Title = gp.Value, DetailValues = new Dictionary<string, object>(), Key = gp.Key };
                if (status >= KpiStatus.Submit)
                {
                    InitDetailDatas(gp.Key, detail.DetailValues, status);
                }
                InitDetailDatas(gp.Key, detail.DetailValues);

                list.Add(detail);
            }
            return list;
        }

        public ActionResult Simulate()
        {
            var id = Request[FORM_KEY];
            var ent= Dao.Get().Query<KPIEntity>().FirstOrDefault(o => o.Id == id);
            var loginId = DBContext<IUser>.Instance.FirstOrDefault(c => c.Id == ent.Approvaler).LoginId;
            FormsAuthentication.SignOut();
            FormsAuthentication.SetAuthCookie(loginId, false);
            return Redirect("/KPI/Index?{0}={1}".FormatTo(FORM_KEY,id));
        }

        private void InitDetailDatas(string gp,IDictionary<string,object> datas, KpiStatus status= KpiStatus.Init)
        {
            var approveType = status >= KpiStatus.Submit ? "Approve" : "Self";
            foreach (var ti in typeArr)
            {
                datas[$"{gp}-{ti}-{approveType}"] = string.Empty;
            }
        }

        public JsonResult Save()
        {
            var dict = Request.Form.ToDictionary();
            var creatorId = dict["CreatorId"].ToString()??Sec.User.Id;
            var isNew = dict["IsNew"].ConvertTo<bool>();
            var kpiMonth ="{0}-01".FormatTo(dict["KPIMonth"]).ConvertTo<DateTime>();
            var id = Request[KPI_KEY];
            KPIEntity kpiEntity = null;
            if (id.IsNullOrEmpty())
            {
                kpiEntity = Dao.Get().Query<KPIEntity>().FirstOrDefault(o => o.CreatorId == creatorId && o.KPIMonth == kpiMonth);
            }
            else
            {
                kpiEntity = Dao.Get().Query<KPIEntity>().FirstOrDefault(o => o.Id == id);
                var monEntity= Dao.Get().Query<KPIEntity>().FirstOrDefault(o => o.CreatorId == creatorId && o.KPIMonth == kpiMonth);
                if (monEntity!=null&& kpiEntity.Id != monEntity.Id)
                {
                    throw new Exception($"已存在{dict["KPIMonth"]}的填写记录，无法保存");
                }
            }
            if (!isNew && kpiEntity == null)//标记不为New，但查找不到相关记录
            {
                throw new Exception("无法找到相关记录");
            }
            if (isNew && kpiEntity != null)//标记为New，但找到记录，会覆盖
            {
                throw new Exception("该月已填写过相关记录，无法新增");
            }
            if (!dict.ContainsKey("Approvaler") || dict["Approvaler"] == null || dict["Approvaler"].ToString().IsNullOrEmpty())
            {
                throw new Exception("请选择审核者");
            }

            using (TransactionScope tran = new TransactionScope())
            {
                if (kpiEntity == null)
                {
                    kpiEntity = new KPIEntity() { CreatorId = Sec.User.Id, CreateTime = DateTime.Now, KPIMonth = kpiMonth.ConvertTo<DateTime>(), Id = Utility.GetGuid(), Approvaler = dict["Approvaler"].ToString() };
                    Dao.Get().Insert(kpiEntity);
                }
                else
                {
                    kpiEntity.Approvaler = dict["Approvaler"].ToString();
                    kpiEntity.KPIMonth = kpiMonth;
                    Dao.Get().Update(kpiEntity);
                }
                Dao.Get().Delete<KPIDetailEntity>().Where(o => o.KPIId == kpiEntity.Id).Execute();
                foreach (var item in dict.Where(c => c.Key.Contains('-')))
                {
                    if (item.Value == null || item.Value.ToString().IsNullOrEmpty() || item.Value.ToString().Trim().IsNullOrEmpty())
                    {
                        continue;
                    }
                    var kpiDetail = new KPIDetailEntity() { Id = Utility.GetGuid(), Key = item.Key, Value = item.Value.ToString(), KPIId = kpiEntity.Id, RentId = 1 };
                    Dao.Get().Insert(kpiDetail);
                }
                tran.Complete();
            }
            return Json(new { Result = true,KpiId=kpiEntity.Id });
        }

        [HttpPost]
        public JsonResult ExecuteApprove()
        {
            Save();
            var kpiId = Request[KPI_KEY];
            var kpiStatus = Request[KPI_APPROVE_STATUS];
            if (kpiId.IsNullOrEmpty() || kpiStatus.IsNullOrEmpty())
            {
                throw new Exception("上传的审核信息不完整");
            }
            var kpiEnt = Dao.Get().Query<KPIEntity>().FirstOrDefault(c => c.Id.Equals(kpiId));
            if (kpiEnt == null)
            {
                throw new Exception("未查到对应的KPI记录");
            }
            var status = kpiStatus.ConvertTo<KpiStatus>();
            if (status < KpiStatus.Approve)
            {
                if (status == KpiStatus.Init)
                {
                    kpiEnt.Status = KpiStatus.Submit;
                }
                else
                {
                    kpiEnt.Status = KpiStatus.Approve;
                    kpiEnt.ApprovalTime = DateTime.Now;
                }
                Dao.Get().Update(kpiEnt);
            }
            return Json(true);
        }


        public FileResult ExportExcel()
        {
            string ctrlId = Request[NamingCenter.PARAM_CTRL_ID];
            string pageId = Request[NamingCenter.PARAM_PAGE_ID];
            string fileName = string.Format("{0}-{1}.xls","KPI汇总", DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss"));
            List<Table> tableList = new List<Table>();
            var Table = PageBuilder.BuildPage(pageId).FindControl(ctrlId) as Table;
            var dep = DBContext<IDepartment>.Instance.FirstOrDefault(c => c.Name.Equals("IT研发中心"));
            
            foreach (var child in dep.Childrens)
            {
                Table tab = Table.Clone() as Table;
                tab.Id = child.Name;
                tab.DataSource = new KPITotalDataSource() { DepartmentIds=child.Name, RoleIds="部门成员" };
                tableList.Add(tab);
            }
            var teamTab = Table.Clone() as Table;
            teamTab.Id = "组长";
            teamTab.DataSource = new KPITotalDataSource() { DepartmentIds=string.Join(",", dep.Childrens.Select(c=>c.Name)) , RoleIds="经理" };
            tableList.Add(teamTab);

            var workbook = new ExcelHelper().ExportToExcel(tableList);
            using (MemoryStream ms = new MemoryStream())
            {
                workbook.Write(ms);
                return File(ms.GetBuffer(), "application/vnd.ms-excel", fileName);
            }
        }
    }

    [DisplayName("KPI数据源")]
    public class KPIDataSource : ListDataSourceBase
    {
        private T GetDataFormPara<T>(string keyName)
        {
            if (!Parameter.IsNullOrEmpty() && Parameter.ContainsKey(keyName))
            {
                return Parameter[keyName].ConvertTo<T>();
            }
            return default(T);
        }

        public DateTime KPIMonth {
            get
            {
                //if (!Parameter.IsNullOrEmpty()&& Parameter.ContainsKey("KPIMonth"))
                //{
                //    return Parameter["KPIMonth"].ConvertTo<DateTime>();
                //}
                //return DateTime.MinValue;
                return GetDataFormPara<DateTime>("KPIMonth");
            }
        }

        public string Status {
            get
            {
                //if (!Parameter.IsNullOrEmpty() && Parameter.ContainsKey("Status"))
                //{
                //    return Parameter["Status"].ToString();
                //}
                //return string.Empty;
                return GetDataFormPara<string>("Status");
            }
        }

        public string UserId {
            get
            {
                return GetDataFormPara<string>("UserId");
            }
        }

        public KpiQueryType KpiQueryType {
            get
            {
                return GetDataFormPara<KpiQueryType>("KpiQueryType");
            }
        }

        public IDepartment Department
        {
            get
            {
                var depId = GetDataFormPara<string>("Department");

                if (depId.IsNullOrEmpty())
                {
                    return Sec.User.Department;
                }
                return DBContext<IDepartment>.Instance.FirstOrDefault(c => c.Id == depId);
            }
        }

        public KpiDataSourceType KpiDataSourceType { get; set; }

        protected override IEnumerable<IDictionary<string, object>> GetListInner()
        {
            IEnumerable<IDictionary<string, object>> result =null;
            switch (KpiDataSourceType)
            {
                case KpiDataSourceType.All:
                    result= Dao.Get().Query<KPIEntity>().ToList().ToDictionary();
                    break;
                case KpiDataSourceType.Init:
                    result= Dao.Get().Query<KPIEntity>().Where(c =>Sec.User.Id==c.CreatorId).OrderByDescending(c=>c.KPIMonth).ToList().ToDictionary();
                    break;
                case KpiDataSourceType.Submit:
                    //IEnumerable<KPIEntity> datas = Dao.Get().Query<KPIEntity>().Where(c => c.Status > KpiStatus.Init && Sec.User.Id == c.Approvaler).ToList();
                    var qDatas = Dao.Get().Query<KPIEntity>();
                    switch (KpiQueryType)
                    {
                        case KpiQueryType.All:
                            if (UserId.IsNotNullOrEmpty())
                            {
                                qDatas = qDatas.Where(c => c.CreatorId == UserId);
                            }
                            break;
                        case KpiQueryType.Self:
                            qDatas = qDatas.Where(c => c.CreatorId == Sec.User.Id);
                            break;
                        case KpiQueryType.Approve:
                            qDatas = qDatas.Where(c => c.Approvaler == Sec.User.Id);
                            break;
                        default:
                            break;
                    }
                    IEnumerable<KPIEntity> datas = qDatas.ToList().Where(c=> 
                         DBContext<IUser>.Instance.FirstOrDefault(u=>u.Id==c.CreatorId).Department.WBS.StartsWith(Department.WBS)
                    );
                    if (KPIMonth != DateTime.MinValue)
                    {
                        datas = datas.Where(c => c.KPIMonth == KPIMonth);
                    }
                    if (Status.IsNotNullOrEmpty())
                    {
                        var status = Status.ConvertTo<KpiStatus>();
                        datas = datas.Where(c => c.Status == status);
                    }
                    result=datas.OrderByDescending(c => c.KPIMonth).ThenByDescending(c => c.ApprovalTime).ToDictionary();
                    break;
                default:
                    return null;
            }
            if (!result.IsNullOrEmpty())
            {
                foreach (var row in result)
                {
                    if (row["ApprovalTime"].ConvertTo<DateTime>() == DateTime.MinValue)
                    {
                        row.Remove("ApprovalTime");
                    }
                    row["KPIMonth"] = row["KPIMonth"].ConvertTo<DateTime>().ToString("yyyy-MM");
                }
            }
            return result;
        }
    }

    /// <summary>
    /// 
    /// </summary>
    [DisplayName("KPI汇总数据源")]
    public class KPITotalDataSource : ListDataSourceBase
    {
        [DisplayName("部门名称")]
        public string DepartmentIds { get; set; }


        [DisplayName("角色名称")]
        public string RoleIds { get; set; }

        public DateTime KPIMonth
        {
            get
            {
                if (!Parameter.IsNullOrEmpty() && Parameter.ContainsKey("KPIMonth"))
                {
                    return Parameter["KPIMonth"].ConvertTo<DateTime>();
                }
                return DateTime.Now.Date.AddDays(1-DateTime.Now.Day);
            }
        }


        public string[] ParaDep
        {
            get
            {
                if (!Parameter.IsNullOrEmpty() && Parameter.ContainsKey("ParaDep"))
                {
                    List<string> result = new List<string>();
                    result.AddRange(DepartmentIds.Split(','));
                    result.AddRange(Parameter["ParaDep"].ToString().Split(','));
                    return result.ToArray();
                }
                return DepartmentIds.Split(',');
            }
        }

        public string[] ParaRole {
            get
            {
                if (!Parameter.IsNullOrEmpty() && Parameter.ContainsKey("ParaRole"))
                {
                    List<string> result = new List<string>();
                    result.AddRange(RoleIds.Split(','));
                    result.AddRange(Parameter["ParaRole"].ToString().Split(','));
                    return result.ToArray();
                }
                return RoleIds.Split(',');
            }
        }

        private static readonly string[] ColumnNames = new string[] { "Summary-Case-Self", "Summary-Score-Self", "Summary-Score-Approve", "Summary-Case-Approve" };
        private static readonly Dictionary<string,string> SummaryNames = new Dictionary<string, string>
        {
            { "Stable-Case-Self" ,"稳定性自评"}, {"Business-Case-Self","业务自评"},{ "Manage-Case-Self","管理自评"}, {"Study-Case-Self","个人成长自评"}
        };

        protected override IEnumerable<IDictionary<string, object>> GetListInner()
        {
            //getMember
            //getkpi主
            //getKPI Detail
            List<string> userList = new List<string>();
            var depIds = ParaDep;
            IEnumerable<IDepartment> deps = DBContext<IDepartment>.Instance.Where(c => ParaDep.Contains(c.Name));
            var roles = ParaRole;
            foreach (var dep in deps)
            {
                if (roles.Contains("部门成员"))
                {
                    userList.AddRange(dep.Member.Where(c=>c.Roles.Count(r=>r.RoleType.Name=="经理")<=0).Select(c => c.Id));
                }
                var curRole = dep.Roles.Where(c => roles.Contains(c.RoleType.Name));
                curRole.ForEach(c => userList.AddRange(c.Members.Select(u => u.Id)));
            }
            var kpiList = Data.Dao.Get().Query<KPIEntity>().Where(c => c.KPIMonth == KPIMonth).ToList().Where(c=>userList.Contains(c.CreatorId)).Select(c=>new { CreatorId = c.CreatorId,KpiId=c.Id }).ToDictionary(c=>c.KpiId);
            var kpiIdList = kpiList.Select(c => c.Key);
            var detailList = Data.Dao.Get().Query<KPIDetailEntity>().ToList().Where(c => kpiIdList.Contains(c.KPIId));//.ToDictionary();
            var result = new List<IDictionary<string, object>>();
            if (!detailList.IsNullOrEmpty())
            {
                var gp = detailList.GroupBy(c => c.KPIId);
                foreach (var g in gp)
                {
                    var row = new Dictionary<string, object>();
                    StringBuilder sb = new StringBuilder();
                    row["CreatorId"] = kpiList[g.First().KPIId].CreatorId;
                    foreach (var col in g)
                    {
                        if (ColumnNames.Contains(col.Key))
                            row[col.Key] = col.Value;
                        if (SummaryNames.ContainsKey(col.Key))
                        {
                            sb.AppendFormat("{0}:{1}\r\n", SummaryNames[col.Key], col.Value);
                        }
                    }
                    row["Summary-Case-Self"] = sb.ToString();
                    result.Add(row);
                }
            }
            return result;
            
        }
    }


    /// <summary>
    /// KPI
    /// </summary>
    [Table("oa_KPI")]
    public class KPIEntity : EntityBase, IAutoCreateTable
    {
        [PrimaryKey]
        public override string Id
        {
            get; set;
        }

        /// <summary>
        /// 创建人
        /// </summary>
        public string CreatorId { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreateTime { get; set; }

        /// <summary>
        /// KPI考核月份
        /// </summary>
        public DateTime KPIMonth { get; set; }


        /// <summary>
        /// 审核者
        /// </summary>
        public string Approvaler { get; set; }

        /// <summary>
        /// 审核时间
        /// </summary>
        [Column(Insert = false)]
        public DateTime ApprovalTime { get; set; }

        /// <summary>
        /// 记录状态：0未提交，1已提交，2已审核
        /// </summary>
        public KpiStatus Status { get; set; }
    }

    /// <summary>
    /// KPIDetail
    /// </summary>
    [Table("oa_KPIDetail")]
    public class KPIDetailEntity : EntityBase, IAutoCreateTable
    {
        [PrimaryKey]
        public override string Id
        {
            get; set;
        }

        /// <summary>
        /// KPIId
        /// </summary>
        public string KPIId { get; set; }

        /// <summary>
        /// Key
        /// </summary>
        public string Key { get; set; }

        /// <summary>
        /// Value
        /// </summary>
        public string Value { get; set; }


    }

    [Description("KPI审核状态")]
    public enum KpiStatus:int
    {
        [Description("未提交")]
        Init=0,
        [Description("未审核")]
        Submit =1,
        [Description("已审核")]
        Approve =2,
    }


    public enum KpiDataSourceType
    {
        [Description("所有")]
        All,
        [Description("自评")]
        Init,
        [Description("审核")]
        Submit
    }

    public enum KpiQueryType
    {
        [Description("提交给我")]
        Approve,
        [Description("查所有")]
        All,
        [Description("查自己")]
        Self,
    }

    public class KpiDetailViewModel
    {
        public string Title { get; set; }

        public string Key { get; set; }

        public Dictionary<string,object> DetailValues { get; set; }
    }
    
    public class KpiViewModel
    {
        public KPIEntity KPIEntity { get; set; }

        public IEnumerable<KpiDetailViewModel> DetailList { get; set; }

        public bool SelfEditable { get; set; }

        public bool ApproveEditable { get; set; }

        public bool Optionable { get; set; }

        public string ApprovorName { get; set; }

        public string CreatorName { get; set; }

        public bool IsNew { get; set; }
    }
}