using FoxOne.Business;
using FoxOne.Core;
using FoxOne.Data;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Transactions;

namespace FoxOne._3VJ
{
    /// <summary>
    /// 会议室预订一览
    /// </summary>
    [DisplayName("会议室预订一览")]
    public class MeetingRoomBookPreview : ListDataSourceBase
    {
        

        private const string DictionaryCode = "MeetingRoom";

        private const string BaseTimeKey = "BaseTime";

        private const string TableHeadTitle = "会议室名称";

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
            var meetingroomList = Dao.Get().Query<DataDictionary>().FirstOrDefault(o => o.Code == DictionaryCode).Items;
            var result = new List<IDictionary<string, object>>();
            var meetingroomBook = Dao.Get().Query<MeetingRoomBookEntity>().Where(o => o.BeginTime > beginDate && o.BeginTime <= endDate || o.EndTime >= beginDate && o.EndTime <= endDate || o.BeginTime < beginDate && o.EndTime > endDate).ToList();
            string bookInfoString = string.Empty;
            int availableHour = 9;
            MeetingRoomBookEntity lastBook = null;
            foreach (var item in meetingroomList)
            {
                var temp = new Dictionary<string, object>();
                temp.Add(TableHeadTitle, item.Name);
                for (var i = beginDate; i < endDate; i = i.AddDays(1))
                {
                    bookInfoString = string.Empty;
                    lastBook = null;
                    var bookInfo = meetingroomBook.Where(o => TimeInRange(i, o) && o.MeetingRoomId.Equals(item.Code, StringComparison.OrdinalIgnoreCase));
                    if (!bookInfo.IsNullOrEmpty())
                    {
                        lastBook = bookInfo.OrderByDescending(o => o.EndTime).First();
                        availableHour = lastBook.EndTime.Hour;
                        foreach (var bi in bookInfo.OrderBy(o => o.BeginTime))
                        {
                            bookInfoString += GetBookDisplay(bi, i);
                        }
                    }
                    else
                    {
                        availableHour = DateTime.Now.Hour + 1;
                    }
                    if (i.Date < DateTime.Now.Date || lastBook != null && lastBook.EndTime.Date > GetDayOfEnd(i))
                    {
                        bookInfoString += "<p></p>";
                    }
                    else
                    {
                        bookInfoString += GetAddDisplay(item, i, availableHour);
                    }
                    temp.Add(i.ToString("yyyy-MM-dd") + "<br />" + SysConfig.DayOfWeekCN[(int)i.DayOfWeek], bookInfoString);
                }
                result.Add(temp);
            }
            return result;
        }

        private string GetBookDisplay(MeetingRoomBookEntity bi, DateTime i)
        {
            var user = DBContext<IUser>.Instance.FirstOrDefault(o => o.Id.Equals(bi.BookUserId));
            string name = user == null ? "未知" : user.Name;
            string displayLabel = string.Empty;
            if (bi.BeginTime < GetDayOfStart(i) && bi.EndTime > GetDayOfEnd(i))
            {
                displayLabel = $"{name}（全天）";
            }
            else
            {
                string startTime = bi.BeginTime < GetDayOfStart(i) ? "开始" : bi.BeginTime.ToString("HH:mm");
                string endTime = bi.EndTime > GetDayOfEnd(i) ? "结束" : bi.EndTime.ToString("HH:mm");
                displayLabel = $"{name}<br />{startTime}至{endTime}";
            }
            return $"<a data-_FORM_KEY='{bi.Id}' data-_FORM_VIEW_MODE='view' class='btn btn-success nobook-item'>{displayLabel}</a>";
        }

        private string GetAddDisplay(DataDictionary item, DateTime i, int availableHour)
        {
            string startTime = i.ToString($"yyyy-MM-dd {availableHour}:00:00");
            string endTime = i.ToString($"yyyy-MM-dd {availableHour + 1}:00:00");
            return $"<p class='btn btn-warning nobook-item' data-meetingroomid='{ item.Code}' data-begintime='{startTime}' data-endtime='{endTime}'>点击预订</p>";
        }

        private bool TimeInRange(DateTime i, MeetingRoomBookEntity o)
        {
            return TimeInRange(i, o.BeginTime) || TimeInRange(i, o.EndTime) || (o.BeginTime < i && o.EndTime > i);
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

    public class MeetingRoomBookDataSource : CRUDDataSource
    {
        public override int Insert(IDictionary<string, object> data)
        {
            var beginTime = data["BeginTime"].ConvertTo<DateTime>();
            var endTime = data["EndTime"].ConvertTo<DateTime>();
            if (beginTime >= endTime)
            {
                throw new FoxOneException("会议开始时间必须小于会议结束时间！");
            }
            if (beginTime < DateTime.Now || endTime < DateTime.Now)
            {
                throw new FoxOneException("会议预订时间已过");
            }
            var meetingroomBook = Dao.Get().Query<MeetingRoomBookEntity>().Where(o => o.MeetingRoomId == data["MeetingRoomId"].ToString()).ToList();// new { MeetingRoomId = data["MeetingRoomId"].ToString() });
            if (meetingroomBook.Any(o => BookTimeConflict(beginTime, endTime, o)))
            {
                throw new FoxOneException("会议预订时间有冲突！");
            }
            return base.Insert(data);
        }

        private bool BookTimeConflict(DateTime beginTime, DateTime endTime, MeetingRoomBookEntity o)
        {
            if (o.BeginTime <= beginTime && o.EndTime > beginTime) return true;
            if (o.BeginTime < endTime && o.EndTime >= endTime) return true;
            if (beginTime <= o.BeginTime && endTime >= o.EndTime) return true;
            if (beginTime >= o.BeginTime && endTime <= o.EndTime) return true;
            return false;
        }
    }
}
