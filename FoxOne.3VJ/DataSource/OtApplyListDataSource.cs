using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using FoxOne.Business;
using FoxOne.Data;
using FoxOne.Workflow.DataAccess;
using FoxOne.Workflow.Kernel;
using FoxOne.Business.Security;
using FoxOne.Core;

namespace FoxOne._3VJ
{
    /// <summary>
    /// 加班调休申请信息数据源
    /// </summary>
    [DisplayName("加班调休申请信息数据源")]
    public class OtApplyListDataSource : ListDataSourceBase
    {
        public DateTime baseBeginTime
        {
            get
            {
                if (!Parameter.IsNullOrEmpty() && Parameter.ContainsKey("BaseTime") && Parameter["BaseTime"] != null)
                {
                    return System.Convert.ToDateTime(Parameter["BaseTime"]);
                }
                return new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            }
        }

        public DateTime baseEndTime
        {
            get
            {
                if (!Parameter.IsNullOrEmpty() && Parameter.ContainsKey("BaseTime") && Parameter["BaseTime"] != null)
                {
                    return System.Convert.ToDateTime(Parameter["BaseTime"]).AddMonths(1).AddSeconds(-1);
                }
                return new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1).AddMonths(1).AddSeconds(-1);
            }
        }

        public string CreatorId
        {
            get
            {
                if (!Parameter.IsNullOrEmpty() && Parameter.ContainsKey("CreatorId") && Parameter["CreatorId"] != null)
                {
                    return Parameter["CreatorId"].ToString();
                }
                return string.Empty;
            }
        }

        public string OTType
        {
            get
            {
                if (!Parameter.IsNullOrEmpty() && Parameter.ContainsKey("OTType") && Parameter["OTType"] != null)
                {
                    return Parameter["OTType"].ToString();
                }
                return string.Empty;
            }
        }

        public string IsSuccess
        {
            get
            {
                if (!Parameter.IsNullOrEmpty() && Parameter.ContainsKey("IsSuccess") && Parameter["IsSuccess"] != null)
                {
                    return Parameter["IsSuccess"].ToString();
                }
                return string.Empty;
            }
        }

        protected override IEnumerable<IDictionary<string, object>> GetListInner()
        {
            var datas = Dao.Get().QueryDictionaries("OtApplyList", new { baseBeginTime = this.baseBeginTime, baseEndTime = this.baseEndTime, CreatorId = this.CreatorId, OTType = this.OTType });
            if (Sec.Provider.HasPermission("OtApplyList_View_Depart"))
            {
                datas = datas.Where(u => u["WBS"].ToString().StartsWith(Sec.User.Department.WBS)).ToList();
            }
            if (!string.IsNullOrEmpty(IsSuccess))
            {
                if (IsSuccess == "1")
                {
                    datas = datas.Where(u => u["FlowTag"].ToString() == "2").ToList();
                }
                else
                {
                    datas = datas.Where(u => u["FlowTag"].ToString() != "2").ToList();
                }
            }
            return datas.OrderBy(u => u[SortExpression == "" ? "WBS" : SortExpression]);
        }

    }

    public enum OTType
    {
        [Description("加班")]
        OverTime = 1,

        [Description("调休")]
        RestTime = 2,
    }
}
