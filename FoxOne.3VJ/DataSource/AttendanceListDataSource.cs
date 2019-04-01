using FoxOne.Business;
using FoxOne.Data.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Transactions;
using FoxOne.Core;
using FoxOne.Data;
using FoxOne.Controls;
using FoxOne.Business.Environment;
using FoxOne.Business.Security;
using FoxOne._3VJ.DataSource;
using FoxOne.Workflow.Business;

namespace FoxOne._3VJ
{
    /// <summary>
    /// 考勤打卡信息数据源
    /// </summary>
    [DisplayName("考勤打卡信息数据源")]
    public class AttendanceListDataSource : ListDataSourceBase /*, IAutoGenerateColumn*/
    {

        private const string BaseTimeKey = "BaseTime";

        private const string TableHeadTitle = "姓名";

        private const string FirstLevelDept = "一级部门";

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

        public string UserId
        {
            get
            {
                if (!Parameter.IsNullOrEmpty() && Parameter.ContainsKey("UserId"))
                {
                    return Parameter["UserId"].ToString();
                }
                return string.Empty;
            }
        }

        public bool OnlyMe
        {
            get
            {
                if (!Parameter.IsNullOrEmpty() && Parameter.ContainsKey("OnlyMe"))
                {
                    return Parameter["OnlyMe"].Equals("只查本人");
                }
                return true;
            }
        }

        protected override IEnumerable<IDictionary<string, object>> GetListInner()
        {
            var dayOfWeek = BaseTime.DayOfWeek;
            var beginDate = new DateTime(BaseTime.Year, BaseTime.Month, BaseTime.Day, 0, 0, 1);

            var endDate = beginDate.AddMonths(1).AddDays(-1);
            endDate = new DateTime(endDate.Year, endDate.Month, endDate.Day, 23, 59, 59);

            IEnumerable<IUser> userList = null;
            if (OnlyMe)
            {
                userList = DBContext<IUser>.Instance.Where(c => c.Id.Equals(Sec.User.Id));
            }
            else
            {
                if (Sec.Provider.HasPermission("VIEW_ALL_ATTENDANCE"))
                {
                    userList = DBContext<IUser>.Instance.Where(o => o.Status.Equals(DefaultStatus.Enabled.ToString(), StringComparison.OrdinalIgnoreCase)).OrderBy(c => c.Department.Parent.Rank).ThenBy(c => c.Department.Rank).AsEnumerable();
                }
                else if (Sec.Provider.HasPermission("VIEW_CENTER_ATTENDANCE"))
                {
                    userList = DBContext<IUser>.Instance.Where(o => o.Status.Equals(DefaultStatus.Enabled.ToString(), StringComparison.OrdinalIgnoreCase) && o.Department.WBS.StartsWith(Sec.User.Department.WBS.Substring(0, 6))).OrderBy(c => c.Department.Parent.Rank).ThenBy(c => c.Department.Rank).AsEnumerable();
                }
                else if (Sec.Provider.HasPermission("VIEW_SELF_DEPT"))
                {
                    userList = DBContext<IUser>.Instance.Where(o => o.Status.Equals(DefaultStatus.Enabled.ToString(), StringComparison.OrdinalIgnoreCase) && o.Department.WBS.StartsWith(Sec.User.Department.WBS)).OrderBy(c => c.Department.Parent.Rank).ThenBy(c => c.Department.Rank).AsEnumerable();
                }
                else
                {
                    userList = DBContext<IUser>.Instance.Where(c => c.Id.Equals(Sec.User.Id));
                }
            }
            string userId = UserId;
            if (UserId.IsNotNullOrEmpty() || OnlyMe)
            {
                if (OnlyMe)
                {
                    userId = Sec.User.WorkNumber;
                }
                else
                {
                    userId = DBContext<IUser>.Instance.FirstOrDefault(o => o.Id.Equals(UserId)).WorkNumber;
                    userList = userList.Where(c => c.Id.Equals(UserId));
                }
            }

            var leaveList = Dao.Get().QueryEntities<UserKq>("select m.Badgenumber as UserId,c.CheckTime, c.CheckType from oa_checkinout c inner join oa_checkinout_idmap m on c.UserId=m.UserId where c.CheckTime>#Checktime# and c.CheckTime<=#EndDate# {? and m.Badgenumber=#UserId# } order by CheckTime desc", new { Checktime = beginDate, UserId = userId, EndDate = endDate.AddDays(1) });

            var otList = Dao.Get().QueryEntities<OTApplyWFEntity>("select l.*,i.FlowTag,i.Description,i.id AS WFInstanceId from wf_form_otapply l inner join wfl_instance i on l.id=i.datalocator where  ((l.OTBeginTime>=#beginDate# and l.OTBeginTime<=#endDate#) OR (l.OTEndTime>=#beginDate# and l.OTEndTime<=#endDate#)) ", new { beginDate = beginDate, endDate = endDate });
            var leaveList1 = Dao.Get().QueryEntities<WFormLeaveApprovalWFEntity>("SELECT a.*,i.FlowTag,i.Description,i.id AS WFInstanceId from WF_Form_LeaveApproval AS a INNER JOIN WFL_Instance AS i on a.id=i.datalocator where ((a.BeginTime>=#beginDate# and a.BeginTime<=#endDate#) OR (a.EndTime>=#beginDate# and a.EndTime<=#endDate#))", new { beginDate = beginDate, endDate = endDate });

            if (DepartmentId.IsNotNullOrEmpty())
            {
                userList = userList.Where(o => o.Department.WBS.StartsWith(DepartmentId));
            }
            var result = new List<IDictionary<string, object>>();
            var dic = new Dictionary<DateTime, List<UserKq>>();
            leaveList.ForEach(c =>
            {
                if (!dic.ContainsKey(c.CheckTime.Date))
                {
                    dic[c.CheckTime.Date] = new List<UserKq>();
                }
                dic[c.CheckTime.Date].Add(c);
            });
            foreach (var item in userList)
            {
                //var temp = new Dictionary<string, object>();
                //temp.Add(TableHeadTitle, item.Name);
                //temp.Add(FirstLevelDept, item.Department.Parent.Name);
                //for (var i = beginDate; i <= endDate; i = i.AddDays(1))
                //{
                //    //temp.Add("日期", $"{i.ToString("dd")}号（{SysConfig.DayOfWeekCN[(int)i.DayOfWeek]}）");
                //    //temp.Add($"{i.ToString("dd")}号（{SysConfig.DayOfWeekCN[(int)i.DayOfWeek]}）", $"{i.ToString("dd")}号（{SysConfig.DayOfWeekCN[(int)i.DayOfWeek]}）");

                //    temp.Add($"{i.ToString("dd")}号（{SysConfig.DayOfWeekCN[(int)i.DayOfWeek]}）_上班", GetDisplay(i.Date, item, true, dic));
                //    temp.Add($"{i.ToString("dd")}号（{SysConfig.DayOfWeekCN[(int)i.DayOfWeek]}）_下班", GetDisplay(i.Date, item, false, dic));

                //}
                //result.Add(temp);

                for (var i = beginDate; i <= endDate; i = i.AddDays(1))
                {
                    var temp = new Dictionary<string, object>();
                    temp.Add(TableHeadTitle, item.Name);
                    temp.Add(FirstLevelDept, GetFirstLevelDeptName(item));
                    temp.Add("日期", $"{i.ToString("dd")}号（{SysConfig.DayOfWeekCN[(int)i.DayOfWeek]}）");
                    temp.Add("上班", GetDisplay(i.Date, item, true, dic));
                    temp.Add("下班", GetDisplay(i.Date, item, false, dic));
                    temp.Add("请假/加班/调休/补考勤", GetSummary(i, item, otList, leaveList1));
                    result.Add(temp);
                }
            }
            return result;
        }

        private string GetStyle(string text, Workflow.Kernel.FlowStatus status,string insId)
        {
            string result = string.Empty;
            switch (status)
            {
                case Workflow.Kernel.FlowStatus.Begin:
                    result = $"<span class='blue' insId='{insId}'>{text}（拟稿）</span>";
                    break;
                case Workflow.Kernel.FlowStatus.Running:
                    result = $"<span class='blue' insId='{insId}'>{text}（审批中）</span>";
                    break;
                case Workflow.Kernel.FlowStatus.Finished:
                    result = $"<span class='green' insId='{insId}'>{text}（审批通过）</span>";
                    break;
                case Workflow.Kernel.FlowStatus.Terminated:
                    result = $"<span class='red' insId='{insId}'>{text}（审批不通过）</span>";
                    break;
                default:
                    break;
            }
            return result;
        }

        private string GetSummary(DateTime i, IUser item, IList<OTApplyWFEntity> otList, IList<WFormLeaveApprovalWFEntity> leaveList)
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
                        res.Add(GetStyle(OT + o.OTTimeLast + "小时", o.FlowTag, o.WFInstanceId));
                    }
                    ///调休
                    else if (o.OTType == 2 && !res.Contains(PL))
                    {
                        if (i.DayOfWeek == DayOfWeek.Sunday)
                        {
                            continue;
                        }
                        res.Add(GetStyle(PL + o.OTTimeLast + "小时", o.FlowTag, o.WFInstanceId));
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
                            res.Add(GetStyle(content, o.FlowTag, o.WFInstanceId));
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
                            res.Add(GetStyle(content, o.FlowTag, o.WFInstanceId));
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
                        res.Add(GetStyle($"{l.Type}{l.LeaveDays}天", l.FlowTag, l.WFInstanceId));
                    }
                    else if (l.BeginTime.Date == i.Date)//当天在请假期间开始
                    {
                        var leaveHour = l.BeginTime.Hour > 12 ? (l.BeginTime.Date.AddHours(18) - l.BeginTime).TotalHours : ((l.BeginTime.Date.AddHours(12) - l.BeginTime).TotalHours + 4);
                        res.Add(GetStyle($"{l.Type}{leaveHour}小时", l.FlowTag, l.WFInstanceId));
                    }
                    else if (l.EndTime.Date == i.Date)//当天在请假期间结束
                    {
                        var leaveHour = l.EndTime.Hour < 12 ? (l.EndTime - l.EndTime.Date.AddHours(8)).TotalHours : ((l.EndTime - l.EndTime.Date.AddHours(14)).TotalHours + 3);
                        res.Add(GetStyle($"{l.Type}{leaveHour}小时", l.FlowTag, l.WFInstanceId));
                    }
                    else
                    {
                        res.Add(GetStyle(l.Type + "（全天）", l.FlowTag, l.WFInstanceId));
                    }
                }
            }
            return res.Count > 0 ? string.Join(",", res.ToArray()) : string.Empty;
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

        //public string GetDisplay(DateTime i, IUser item, bool morning, IList<UserKq> leaveList)
        //{
        //    //List<string> res = new List<string>();
        //    //return res.Count > 0 ? string.Join(",", res.ToArray()) : string.Empty;
        //    if (morning)
        //    {
        //        var curData = leaveList.Where(c => c.UserId.Equals(item.WorkNumber) && c.CheckTime.Date == i.Date && c.CheckTime.Hour >= 6 && c.CheckTime.Hour < 11);
        //        if (!curData.IsNullOrEmpty())
        //        {
        //            return curData.Min(c => c.CheckTime).ToString("HH:mm");
        //        }
        //    }
        //    else
        //    {
        //        var curData = leaveList.Where(c => c.UserId.Equals(item.WorkNumber) &&
        //          (c.CheckTime.Date == i.Date && c.CheckTime.Hour >= 18) ||
        //          ((c.CheckTime.Day - i.Day) == 1 && c.CheckTime.Hour < 6));
        //        if (!curData.IsNullOrEmpty())
        //        {
        //            return curData.Max(c => c.CheckTime).ToString("HH:mm");
        //        }
        //    }
        //    return string.Empty;
        //}

        public string GetDisplay(DateTime i, IUser item, bool morning, Dictionary<DateTime, List<UserKq>> leaveList)
        {
            if (morning)
            {
                if (leaveList.ContainsKey(i))
                {
                    var curData = leaveList[i].Where(c => c.UserId.Equals(item.WorkNumber) && c.CheckTime.Date == i.Date && c.CheckTime.Hour >= 6 && c.CheckTime.Hour < 11);
                    if (!curData.IsNullOrEmpty())
                    {
                        return curData.Min(c => c.CheckTime).ToString("HH:mm");
                    }
                }
            }
            else
            {
                var earlist = DateTime.MinValue;

                if (leaveList.ContainsKey(i))
                {
                    var curData = leaveList[i].Where(c => c.UserId.Equals(item.WorkNumber) && c.CheckTime.Date == i.Date && c.CheckTime.Hour >= 6 && c.CheckTime.Hour < 11);
                    if (!curData.IsNullOrEmpty())
                    {
                        earlist = curData.Min(c => c.CheckTime);
                    }
                }


                var res = string.Empty;
                if (leaveList.ContainsKey(i))
                {
                    var curData = leaveList[i].Where(c => c.UserId.Equals(item.WorkNumber) && c.CheckTime.Hour > 12 && c.CheckTime > earlist);
                    if (!curData.IsNullOrEmpty())
                    {
                        res = curData.Max(c => c.CheckTime).ToString("HH:mm");
                    }
                }
                if (leaveList.ContainsKey(i.AddDays(1)))
                {
                    var curData = leaveList[i.AddDays(1)].Where(c => c.UserId.Equals(item.WorkNumber) && c.CheckTime.Hour < 6);
                    if (!curData.IsNullOrEmpty())
                    {
                        res = "次日凌晨" + curData.Max(c => c.CheckTime).ToString("HH:mm");
                    }
                }
                return res;
            }
            return string.Empty;
        }

        public void GenerateColumn(IList<TableColumn> Columns, string[] keys)
        {
            foreach (var key in keys)
            {
                if (key.Contains('_'))
                {
                    var hArr = key.Split('_');
                    var fCol = Columns.FirstOrDefault(c => c.ColumnName.Equals(hArr[0]));
                    if (fCol == null)
                    {
                        fCol = new TableColumn() { Id = hArr[0], FieldName = hArr[0], Colspan = 2, ColumnName = hArr[0], Children = new List<TableColumn>() };
                        Columns.Add(fCol);
                    }
                    var col = Columns.FirstOrDefault(c => c.ColumnName.Equals(key));
                    if (col == null)
                    {
                        col = new TableColumn() { Id = key, FieldName = key, ColumnName = key };
                    }
                    else
                    {
                        Columns.Remove(col);
                    }
                    col.ColumnName = hArr[1];
                    col.ColumnWidth = "51px";
                    fCol.Children.Add(col);
                }
                else
                {
                    Columns.Add(new TableColumn() { ColumnName = key, Id = key, FieldName = key });
                }
            }
        }
    }

    /// <summary>
    /// 考勤信息
    /// </summary>
    [Table("oa_checkinout")]
    public class UserKq
    {
        [PrimaryKey]
        public string Id { get; set; }

        public string UserId { get; set; }

        public DateTime CheckTime { get; set; }

        public string CheckType { get; set; }
    }

    public class OTApplyWFEntity : OTApplyEntity
    {
        public Workflow.Kernel.FlowStatus FlowTag { get; set; }

        public string Description { get; set; }

        public string WFInstanceId { get; set; }
    }

    public class WFormLeaveApprovalWFEntity : WFormLeaveApprovalEntity
    {
        public Workflow.Kernel.FlowStatus FlowTag { get; set; }

        public string Description { get; set; }

        public string WFInstanceId { get; set; }
    }
}
