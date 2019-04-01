using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FoxOne.Business;
using FoxOne.Core;

namespace FoxOne.FoxHunter
{
    [Category("FoxHunter")]
    [DisplayName("项目看板数据源")]
    public class ProjectViewDataSrouce : ListDataSourceBase
    {
        private const string PROJECT_ID_KEY = "ProjectId";
        private const string NA = "NA";

        [DisplayName("目标类型")]
        public ProjectViewEnum DataSrouceTarget { get; set; }

        private string projectId;

        public string ProjectId {
            get {
                if (projectId.IsNullOrEmpty())
                {
                    if (!this.Parameter.IsNullOrEmpty()&& this.Parameter.ContainsKey(PROJECT_ID_KEY))
                    {
                        projectId= this.Parameter[PROJECT_ID_KEY].ToString();
                    }
                    else
                    {
                        projectId = Data.Dao.Get().QueryScalar<string>("SELECT Id FROM proj_project limit 1");
                    }
                }
                return projectId;
            }
        }

        protected override IEnumerable<IDictionary<string, object>> GetListInner()
        {
            switch (this.DataSrouceTarget)
            {
                default:
                case ProjectViewEnum.Title:
                    return GetProjectTitle();
                case ProjectViewEnum.Detail:
                    return GetProjectParamList();
                case ProjectViewEnum.WorkList:
                    return GetWorkTimeList();
            }
        }

        private IEnumerable<IDictionary<string,object>> GetWorkTimeList()
        {
            var datas = Data.Dao.Get().Query<WorkTimeEntity>().Where(c => c.ProjectId == ProjectId && c.Status == WorktimeStatus.Pass).ToList().GroupBy(c => c.UserId);
            var result = new List<IDictionary<string, object>>();
            foreach (var g in datas)
            {
                var user= DBContext<IUser>.Instance.FirstOrDefault(c => c.Id.Equals(g.Key, StringComparison.CurrentCultureIgnoreCase));
                var row = new Dictionary<string, object>();
                row["UserId"] = g.Key;
                row["UserName"] = user.Name;
                row["Salary"] = user.Salary;
                row["WorkTime"] = g.Sum(c => (c.EndTime - c.StartTime).TotalMinutes);
                result.Add(row);
            }
            return result;
        }

        private IEnumerable<IDictionary<string, object>> GetProjectParamList()
        {
            var datas = Data.Dao.Get().Query<ProjectParameterEntity>().Where(c => c.ProjectId == ProjectId).ToList();
            string key = $"ProjectParaResList_{ProjectId}";
            return CacheHelper.GetFromCache<IEnumerable<IDictionary<string, object>>>(key,() =>
            {
                var project = Project;
                var result = new List<IDictionary<string, object>>();
                foreach (var param in datas)
                {
                    var row = new Dictionary<string, object>();
                    var calIns = param.CalculatorInstance;
                    calIns.Context = new CalculatorContext()
                    {
                        Parameter = param,
                        Project = project,
                    };

                    row["Name"] = param.Name;
                    if (calIns.Calculate())
                    {
                        row["Money"] =Math.Round( calIns.Money,2);
                        row["Rate"] =Math.Round( calIns.Rate,2);
                        row["IsOverLimit"] = calIns.IsOverLimit();
                    }
                    else
                    {
                        row["Money"] = "NA";
                        row["Rate"] = "NA";
                        row["IsOverLimit"] = NA;
                    }
                    result.Add(row);
                }
                return result;
            },DateTime.Now.AddSeconds(5));
        }

        private IEnumerable<IDictionary<string,object>> GetProjectTitle()
        {
            var project = Project;
            var pMoney = project["TotalMoney"].ConvertTo<decimal>();
            var pCost = this.GetProjectParamList().Sum(c => c["Money"].ConvertTo<decimal>());
            var profit = pMoney - pCost;
            var pRate = profit / pMoney;
            project["TotalCost"] =Math.Round( pCost,2);
            project["Profit"] = Math.Round(profit,2);
            project["ProfitRate"] = Math.Round( pRate,2);
            project["IsProfit"] = profit > 0;
            return new List<IDictionary<string, object>> { project };
        }

        private IDictionary<string,object> Project
        {
            get
            {
                return Data.Dao.Get().QueryDictionary("select * from proj_project where Id=#ProjectId#", this); 
            }
        }
    }
    
    public enum ProjectViewEnum
    {
        [Description("抬头")]
        Title,

        [Description("明细")]
        Detail,

        [Description("工时表")]
        WorkList,
}
}
