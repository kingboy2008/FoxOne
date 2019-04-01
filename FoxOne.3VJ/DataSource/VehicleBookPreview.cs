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

namespace FoxOne._3VJ.DataSource
{
    /// <summary>
    /// 用车申请一览
    /// </summary>
    [DisplayName("用车申请一览")]
    public class VehicleBookPreviewDataSource : ListDataSourceBase
    {
        private static string[] DayOfWeekCN = new string[] { "星期日", "星期一", "星期二", "星期三", "星期四", "星期五", "星期六" };

        private const string DictionaryCode = "CarInfo";

        private const string BaseTimeKey = "BaseTime";

        private const string TableHeadTitle = "车辆信息";

        public DateTime BaseTime
        {
            get
            {
                if (Parameter == null) return DateTime.Now;
                return Parameter.ContainsKey(BaseTimeKey) ? Parameter[BaseTimeKey].ConvertTo<DateTime>() : DateTime.Now;
            }
        }

        protected override IEnumerable<IDictionary<string, object>> GetListInner()
        {
            var dayOfWeek = BaseTime.DayOfWeek;
            var beginDate = BaseTime.AddDays(1 - (int)dayOfWeek);
            beginDate = new DateTime(beginDate.Year, beginDate.Month, beginDate.Day, 0, 0, 1);
            var endDate = beginDate.AddDays(6);
            endDate = new DateTime(endDate.Year, endDate.Month, endDate.Day, 23, 59, 59);
            var vehicleList = Dao.Get().Query<DataDictionary>().FirstOrDefault(o => o.Code == DictionaryCode).Items;
            var result = new List<IDictionary<string, object>>();
            var vehicleApply = Dao.Get().Query<VehicalApplyEntity>().Where(o => o.UseBeginTime > beginDate && o.UseBeginTime <= endDate || o.UseEndTime >= beginDate && o.UseEndTime <= endDate || o.UseBeginTime < beginDate && o.UseEndTime > endDate).ToList();

            var flows = Workflow.Business.WorkflowHelper.GetAllInstance().Where(o => vehicleApply.Any(v => v.Id == o.DataLocator)).ToDictionary(o => o.DataLocator, f => f.FlowTag);
            string bookInfoString = string.Empty;
            int availableHour = 9;
            VehicalApplyEntity lastBook = null;
            foreach (var item in vehicleList)
            {
                var temp = new Dictionary<string, object>();
                temp.Add(TableHeadTitle, item.Name);
                for (var i = beginDate; i < endDate; i = i.AddDays(1))
                {
                    bookInfoString = string.Empty;
                    lastBook = null;
                    var bookInfo = vehicleApply.Where(o => TimeInRange(i, o) && o.CarLicense.Equals(item.Code, StringComparison.OrdinalIgnoreCase));
                    if (!bookInfo.IsNullOrEmpty())
                    {
                        lastBook = bookInfo.OrderByDescending(o => o.UseEndTime).First();
                        availableHour = lastBook.UseEndTime.Hour;
                        foreach (var bi in bookInfo.OrderBy(o => o.UseBeginTime))
                        {
                            bookInfoString += GetBookDisplay(bi, i, flows);
                        }
                    }
                    else
                    {
                        availableHour = DateTime.Now.Hour + 1;
                    }
                    if (i.Date < DateTime.Now.Date || lastBook != null && lastBook.UseEndTime.Date > GetDayOfEnd(i))
                    {
                        bookInfoString += "&nbsp;";
                    }
                    else
                    {
                        bookInfoString += GetAddDisplay(item, i, availableHour);
                    }
                    temp.Add(i.ToString("yyyy-MM-dd") + "<br />" + DayOfWeekCN[(int)i.DayOfWeek], bookInfoString);
                }
                result.Add(temp);
            }
            return result;
        }

        private string GetBookDisplay(VehicalApplyEntity bi, DateTime i, IDictionary<string, Workflow.Kernel.FlowStatus> flows)
        {
            var user = DBContext<IUser>.Instance.FirstOrDefault(o => o.Id.Equals(bi.CreatorId));
            string name = user == null ? "未知" : user.Name;
            string displayLabel = string.Empty;
            if (bi.UseBeginTime < GetDayOfStart(i) && bi.UseEndTime > GetDayOfEnd(i))
            {
                displayLabel = $"{name}（全天）";
            }
            else
            {
                string startTime = bi.UseBeginTime < GetDayOfStart(i) ? "开始" : bi.UseBeginTime.ToString("HH:mm");
                string endTime = bi.UseEndTime > GetDayOfEnd(i) ? "结束" : bi.UseEndTime.ToString("HH:mm");
                displayLabel = $"{name}<br />{startTime}至{endTime}<br />{flows[bi.Id].GetDescription()}";
            }
            string btnClass = "btn-primary";
            if (flows[bi.Id] == Workflow.Kernel.FlowStatus.Terminated)
            {
                btnClass = "btn-danger";
            }
            else if (flows[bi.Id] == Workflow.Kernel.FlowStatus.Finished)
            {
                btnClass = "btn-success";
            }
            return $"<a data-_FORM_KEY='{bi.Id}' data-_FORM_VIEW_MODE='view' class='nobook-item btn {btnClass}'>{displayLabel}</a>";
        }

        private string GetAddDisplay(DataDictionary item, DateTime i, int availableHour)
        {
            string startTime = i.ToString($"yyyy-MM-dd 09:00:00");
            string endTime = i.ToString($"yyyy-MM-dd 18:00:00");
            return $"<a class='nobook-item btn btn-warning' data-carlicense='{ item.Code}' data-usebegintime='{startTime}' data-useendtime='{endTime}'>申请用车</a>";
        }

        private bool TimeInRange(DateTime i, VehicalApplyEntity o)
        {
            return TimeInRange(i, o.UseBeginTime) || TimeInRange(i, o.UseEndTime) || (o.UseBeginTime < i && o.UseEndTime > i);
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
    }

    /// <summary>
    /// 用车申请数据源
    /// </summary>
    [DisplayName("用车申请数据源")]
    public class VehicalBookDataSource : CRUDDataSource
    {
        public override int Insert(IDictionary<string, object> data)
        {
            var beginTime = data["UseBeginTime"].ConvertTo<DateTime>();
            var endTime = data["UseEndTime"].ConvertTo<DateTime>();
            if (beginTime >= endTime)
            {
                throw new FoxOneException("用车开始时间必须小于用车结束时间！");
            }
            if (beginTime < DateTime.Now || endTime < DateTime.Now)
            {
                throw new FoxOneException("申请用车时间已过");
            }
            var existUnFinishApply = Dao.Get().QueryScalar<int>("SELECT COUNT(0) FROM wf_form_vehicalapply v inner join wfl_instance i on v.Id=i.DataLocator  where i.FlowTag<2 and v.CreatorId=#Id#", new { Id = Sec.User.Id }) > 0;
            if (existUnFinishApply)
            {
                throw new FoxOneException("您还有未完成的用车申请流程，请先完成再申请");
            }
            var meetingroomBook = Dao.Get().QueryEntities<VehicalApplyEntity>("SELECT v.* FROM wf_form_vehicalapply v inner join wfl_instance i on v.Id=i.DataLocator  where i.FlowTag<=2 and v.CarLicense=#CarLicense#", data); //Dao.Get().Query<VehicalApplyEntity>().Where(o => o.CarLicense == data["CarLicense"].ToString()).ToList();
            if (meetingroomBook.Any(o => BookTimeConflict(beginTime, endTime, o)))
            {
                throw new FoxOneException("申请用车时间有冲突！");
            }
            return base.Insert(data);
        }

        private bool BookTimeConflict(DateTime beginTime, DateTime endTime, VehicalApplyEntity o)
        {
            if (o.UseBeginTime <= beginTime && o.UseEndTime > beginTime) return true;
            if (o.UseBeginTime < endTime && o.UseEndTime >= endTime) return true;
            if (beginTime <= o.UseBeginTime && endTime >= o.UseEndTime) return true;
            if (beginTime >= o.UseBeginTime && endTime <= o.UseEndTime) return true;
            return false;
        }
    }

    /// <summary>
    /// 用车申请信息
    /// </summary>
    [DisplayName("用车申请信息")]
    [Table("wf_form_vehicalapply")]
    public class VehicalApplyEntity : EntityBase, IAutoCreateTable
    {
        [PrimaryKey]
        public override string Id
        {
            get; set;
        }

        public string CreatorId { get; set; }

        public string CreatorDeptId { get; set; }

        public string CarLicense { get; set; }

        public DateTime UseBeginTime { get; set; }

        public DateTime UseEndTime { get; set; }

        public DateTime CreateTime { get; set; }
    }
}
