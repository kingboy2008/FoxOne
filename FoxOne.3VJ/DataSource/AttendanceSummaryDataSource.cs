using FoxOne._3VJ.DataSource;
using FoxOne.Business;
using FoxOne.Business.Security;
using FoxOne.Core;
using FoxOne.Data;
using FoxOne.Data.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Transactions;

namespace FoxOne._3VJ
{
    /// <summary>
    /// 考勤汇总信息数据源
    /// </summary>
    [DisplayName("考勤汇总信息数据源")]
    public class AttendanceSummaryDataSource : ListDataSourceBase
    {

        private const string BaseTimeKey = "BaseTime";

        private const string TableHeadTitle = "姓名";

        private const string FirstLevelDept = "一级部门";

        private const string SecondLevelDept = "二级部门";

        private const string OT = "加班";

        private const string PL = "调休";

        /// <summary>
        /// 汇总月份
        /// </summary>
        public DateTime BaseTime
        {
            get
            {
                if (Parameter == null) return DateTime.Now;
                return Parameter.ContainsKey(BaseTimeKey) ? Parameter[BaseTimeKey].ConvertTo<DateTime>() : DateTime.Now;
            }
        }

        /// <summary>
        /// 部门ID
        /// </summary>
        public string DepartmentId
        {
            get
            {
                if (!Parameter.IsNullOrEmpty() && Parameter.ContainsKey("DepartmentId"))
                {
                    return Parameter["DepartmentId"].ToString();
                }
                return string.Empty;
            }
        }

        protected override IEnumerable<IDictionary<string, object>> GetListInner()
        {
            var dayOfWeek = BaseTime.DayOfWeek;
            var beginDate = new DateTime(BaseTime.Year, BaseTime.Month, BaseTime.Day, 0, 0, 1);

            var endDate = beginDate.AddMonths(1).AddDays(-1);
            endDate = new DateTime(endDate.Year, endDate.Month, endDate.Day, 23, 59, 59);
            var userList = DBContext<IUser>.Instance.Where(o => o.Status.Equals(DefaultStatus.Enabled.ToString(), StringComparison.OrdinalIgnoreCase)).
                OrderBy(c => c.Department.Parent.Rank).ThenBy(c => c.Department.Rank).AsEnumerable();

            var otList = Dao.Get().QueryEntities<OTApplyEntity>("select l.* from wf_form_otapply l inner join wfl_instance i on l.id=i.datalocator where i.flowtag=2 and ((l.OTBeginTime>=#beginDate# and l.OTBeginTime<=#endDate#) OR (l.OTEndTime>=#beginDate# and l.OTEndTime<=#endDate#)) ", new { beginDate = beginDate, endDate = endDate });
            var leaveList = Dao.Get().QueryEntities<WFormLeaveApprovalEntity>("SELECT a.* from WF_Form_LeaveApproval AS a INNER JOIN WFL_Instance AS i on a.id=i.datalocator where i.flowtag=2 and ((a.BeginTime>=#beginDate# and a.BeginTime<=#endDate#) OR (a.EndTime>=#beginDate# and a.EndTime<=#endDate#))", new { beginDate = beginDate, endDate = endDate });
            if (DepartmentId.IsNotNullOrEmpty())
            {
                userList = userList.Where(o => o.Department.WBS.StartsWith(DepartmentId));
            }
            var result = new List<IDictionary<string, object>>();

            foreach (var item in userList)
            {
                var temp = new Dictionary<string, object>();
                temp.Add(TableHeadTitle, item.Name);
                if (item.Department != null && item.Department.Parent != null)
                {
                    temp.Add(FirstLevelDept, GetFirstLevelDeptName(item));
                }
                else
                {
                    temp.Add(FirstLevelDept, string.Empty);
                }
                if (item.Department != null)
                {
                    temp.Add(SecondLevelDept, item.Department.Name);
                }
                else
                {
                    temp.Add(SecondLevelDept, string.Empty);
                }
                for (var i = beginDate; i <= endDate; i = i.AddDays(1))
                {
                    temp.Add(i.ToString("dd"), GetDisplay(i, item, otList, leaveList));
                }
                result.Add(temp);
            }
            return result;
        }

        public string GetFirstLevelDeptName(IUser user)
        {
            var dept = user.Department;
            while (dept.Level > 2)
            {
                dept = dept.Parent;
            }
            return dept.Name;
        }

        public string GetDisplay(DateTime i, IUser item, IList<OTApplyEntity> otList, IList<WFormLeaveApprovalEntity> leaveList)
        {
            List<string> res = new List<string>();
            var ot = otList.Where(o => o.CreatorId == item.Id && (o.OTBeginTime.Date <= i.Date && o.OTEndTime.Date >= i.Date));
            if (!ot.IsNullOrEmpty())
            {
                foreach (var o in ot)
                {
                    ///加班
                    if (o.OTType == 1 && !res.Contains(OT))
                    {
                        res.Add(OT + o.OTTimeLast + "小时");
                    }
                    ///调休
                    else if (o.OTType == 2 && !res.Contains(PL))
                    {
                        if (i.DayOfWeek == DayOfWeek.Sunday)
                        {
                            continue;
                        }
                        res.Add(PL + o.OTTimeLast + "小时");
                    }
                    else if (o.OTType >= 30 && o.OTType < 500)//30~500 从数据字典中查找，并仅显示当天
                    {
                        var dic = DBContext<DataDictionary>.Instance.FirstOrDefault(c => c.Code.Equals("KQBD", StringComparison.OrdinalIgnoreCase));
                        if (o.OTBeginTime.Date != i.Date)
                        {
                            continue;
                        }
                        var content = o.OTType.ToString();
                        if (dic.Items.Count(c => c.Code.Equals(content)) > 0)
                        {
                            content = dic.Items.FirstOrDefault(c => c.Code.Equals(content)).Name;
                        }
                        if (!res.Contains(content))
                        {
                            res.Add(content);
                        }
                    }
                    else if (o.OTType >= 500)//500以上 从数据字典中查找，显示的是范围
                    {
                        var dic = DBContext<DataDictionary>.Instance.FirstOrDefault(c => c.Code.Equals("KQBD", StringComparison.OrdinalIgnoreCase));
                        if (i.DayOfWeek == DayOfWeek.Sunday)
                        {
                            continue;
                        }
                        var content = o.OTType.ToString();
                        if (dic.Items.Count(c => c.Code.Equals(content)) > 0)
                        {
                            content = dic.Items.FirstOrDefault(c => c.Code.Equals(content)).Name;
                        }
                        if (!res.Contains(content))
                        {
                            res.Add(content);
                        }
                    }
                }
            }
            var leave = leaveList.Where(o => o.CreatorId.Equals(item.Id) && (o.BeginTime.Date <= i.Date && o.EndTime.Date >= i.Date));
            if (!leave.IsNullOrEmpty())
            {
                foreach (var l in leave)
                {
                    if (i.DayOfWeek == DayOfWeek.Sunday)
                    {
                        continue;
                    }
                    if (l.BeginTime.Date == l.EndTime.Date)//只有当天
                    {
                        res.Add($"{l.Type}{l.LeaveDays}小时");
                    }
                    else if (l.BeginTime.Date == i.Date)//当天在请假期间开始
                    {
                        var leaveHour = l.BeginTime.Hour > 12 ? (l.BeginTime.Date.AddHours(18) - l.BeginTime).TotalHours : ((l.BeginTime.Date.AddHours(12) - l.BeginTime).TotalHours + 4);
                        res.Add($"{l.Type}{leaveHour}小时");
                    }
                    else if (l.EndTime.Date == i.Date)//当天在请假期间结束
                    {
                        var leaveHour = l.EndTime.Hour < 12 ? (l.EndTime - l.EndTime.Date.AddHours(8)).TotalHours : ((l.EndTime - l.EndTime.Date.AddHours(14)).TotalHours + 3);
                        res.Add($"{l.Type}{leaveHour}小时");
                    }
                    else
                    {
                        res.Add(l.Type);
                    }
                }
            }
            return res.Count > 0 ? string.Join(",", res.ToArray()) : string.Empty;
        }
    }
}
