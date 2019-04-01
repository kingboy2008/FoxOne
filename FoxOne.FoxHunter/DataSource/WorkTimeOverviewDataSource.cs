using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FoxOne.Business;
using FoxOne.Business.Security;
using FoxOne.Core;

namespace FoxOne.FoxHunter
{
    [Category("FoxHunter")]
    [DisplayName("工时一览数据源")]
    public class WorkTimeOverviewDataSource:ListDataSourceBase
    {
        private const string BaseTimeKey = "BaseTime";

        private const string ViewTypeKey = "ViewType";

        private const string TableHeadTitle = "姓名";

        public DateTime BaseTime
        {
            get
            {
                if (Parameter == null) return DateTime.Now;
                return Parameter.ContainsKey(BaseTimeKey) ? Parameter[BaseTimeKey].ConvertTo<DateTime>() : DateTime.Now;
            }
        }

        public WorkTimeViewType ViewType
        {
            get
            {
                if (Parameter == null) return  WorkTimeViewType.MySubmit;
                return Parameter.ContainsKey(ViewTypeKey) ? Parameter[ViewTypeKey].ConvertTo<WorkTimeViewType>() :  WorkTimeViewType.MySubmit;
            }
        }

        protected override IEnumerable<IDictionary<string, object>> GetListInner()
        {
            var dayOfWeek = BaseTime.DayOfWeek;
            var beginDate = BaseTime.AddDays(1 - (int)dayOfWeek);
            beginDate = new DateTime(beginDate.Year, beginDate.Month, beginDate.Day, 0, 0, 1);
            var endDate = beginDate.AddDays(6);
            endDate = new DateTime(endDate.Year, endDate.Month, endDate.Day, 23, 59, 59);
            var projectList = Data.Dao.Get().QueryDictionaries("SELECT Id,Name FROM proj_project");
            var projectRoleList = Data.Dao.Get().QueryDictionaries("SELECT * FROM proj_project_user_rel");
            var workItemList = Data.Dao.Get().QueryDictionaries("SELECT Id,Name FROM proj_workitem");
            var workTimeString = string.Empty;
            WorkTimeEntity lastWorkTime = null;
            int availableHour = 24;

            List<WorkTimeEntity> datas = new List<WorkTimeEntity>();
            var result = new List<IDictionary<string, object>>();

            var userList = new Dictionary<string, string>();
            if (ViewType== WorkTimeViewType.MySubmit)
            {
                datas.AddRange(Data.Dao.Get().Query<WorkTimeEntity>().Where(c => c.UserId == Sec.User.Id &&( c.StartTime >= beginDate || c.EndTime <= endDate)).ToList());
                userList[Sec.User.Id] = Sec.User.Name;
            }
            else
            {
                var param = new Dictionary<string, object>() {
                    { "UserId",Sec.User.Id},
                    {"RoleId",WorktimeDatasource.PM_ROLE_ID}
                };
                var projectIdList= Data.Dao.Get().QueryDictionaries("SELECT ProjectId FROM proj_project_user_rel where UserId=#UserId# AND RoleId=#RoleId#", param).Select(c=>c["ProjectId"].ToString()).ToList();
                datas.AddRange(Data.Dao.Get().Query<WorkTimeEntity>().Where(c => (c.StartTime >= beginDate || c.EndTime <= endDate)).ToList().Where(c=>projectIdList.Contains(c.ProjectId) ));
                datas.Distinct(c => c.UserId).ForEach(c => {
                    userList[c.UserId] = DBContext<IUser>.Instance.FirstOrDefault(u => u.Id.Equals(c.UserId)).Name;
                });
            }
            foreach (var item in userList)
            {
                var temp = new Dictionary<string, object>();
                temp.Add(TableHeadTitle, item.Value);
                for (var i = beginDate; i < endDate; i = i.AddDays(1))
                {
                    workTimeString = string.Empty;
                    lastWorkTime = null;
                    var bookInfo = datas.Where(o => TimeInRange(i, o) && o.UserId.Equals(item.Key, StringComparison.OrdinalIgnoreCase));
                    if (!bookInfo.IsNullOrEmpty())
                    {
                        lastWorkTime = bookInfo.OrderByDescending(o => o.EndTime).First();
                        availableHour = lastWorkTime.EndTime.Hour;
                        foreach (var bi in bookInfo.OrderBy(o => o.StartTime))
                        {
                            workTimeString += GetDisplay(bi, i,projectList,projectRoleList,workItemList);
                        }
                    }
                    else
                    {
                        availableHour = DateTime.Now.Hour + 1;
                    }
                    if (ViewType == WorkTimeViewType.MySubmit)
                    {
                        workTimeString +=$"<p class='btn btn-warning nobook-item' >添加工时</p>"; 
                    }
                    else
                    {
                        workTimeString += "<p></p>";
                    }
                    temp.Add(i.ToString("yyyy-MM-dd") + "<br />" + SysConfig.DayOfWeekCN[(int)i.DayOfWeek], workTimeString);
                }
                result.Add(temp);
            }
            return result;

        }
        

        private string GetDisplay(WorkTimeEntity bi, DateTime i,IList<IDictionary<string,object>> projList,IList<IDictionary<string,object>> projectRoleList,IList<IDictionary<string,object>> workitmeList)
        {
            var projectName = projList.FirstOrDefault(c => c["Id"].Equals(bi.ProjectId))["Name"];
            var workItemName = workitmeList.FirstOrDefault(c => c["Id"].Equals(bi.WorkItemId))["Name"];
            string displayLabel = string.Empty;
            if (bi.StartTime < GetDayOfStart(i) && bi.EndTime > GetDayOfEnd(i))
            {
                displayLabel = $"{projectName}<br/>{workItemName}（全天）";
            }
            else
            {
                string startTime = bi.StartTime < GetDayOfStart(i) ? "开始" : bi.StartTime.ToString("HH:mm");
                string endTime = bi.EndTime > GetDayOfEnd(i) ? "结束" : bi.EndTime.ToString("HH:mm");
                displayLabel = $"{projectName}<br/>{workItemName}<br />{startTime}至{endTime}";
            }

            string btnClass = "btn-primary";
            if (bi.Status== WorktimeStatus.Reject)
            {
                btnClass = "btn-danger";
            }
            else if (bi.Status== WorktimeStatus.Pass)
            {
                btnClass = "btn-success";
            }
            string formViewModel = "";
            bool isPM = projectRoleList.Any(c => c["ProjectId"].Equals(bi.ProjectId) && c["UserId"].Equals(Sec.User.Id) && c["RoleId"].Equals(WorktimeDatasource.PM_ROLE_ID));
            if ((!bi.UserId.Equals(Sec.User.Id)&&!isPM) ||bi.Status== WorktimeStatus.Pass)
            {
                formViewModel = "data-_FORM_VIEW_MODE='view'";
            }
            return $"<a data-_FORM_KEY='{bi.Id}' {formViewModel}  class='btn {btnClass} nobook-item'>{displayLabel}</a>";
        }

        private bool TimeInRange(DateTime i, WorkTimeEntity o)
        {
            return TimeInRange(i, o.StartTime) || TimeInRange(i, o.EndTime) || (o.StartTime < i && o.EndTime > i);
        }

        private bool TimeInRange(DateTime i, DateTime bookTime)
        {
            return bookTime > GetDayOfStart(i) && bookTime < GetDayOfEnd(i);
        }

        private DateTime GetDayOfStart(DateTime i)
        {
            return new DateTime(i.Year, i.Month, i.Day, 0, 0, 1);
        }

        private DateTime GetDayOfEnd(DateTime i)
        {
            return new DateTime(i.Year, i.Month, i.Day, 23, 59, 59);
        }

        private string GetColumnHeader(DateTime time)
        {
            return time.ToString("yyyy-MM-dd") + "<br />" + SysConfig.DayOfWeekCN[(int)time.DayOfWeek];
        }
    }

    public enum WorkTimeViewType
    {
        [Description("我提交的")]
        MySubmit,

        [Description("我审核的")]
        MyCheck
    }
}
