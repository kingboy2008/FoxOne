using FoxOne.Business;
using FoxOne.Core;
using FoxOne.Data.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace FoxOne._3VJ
{
    /// <summary>
    /// 会议室预订信息
    /// </summary>
    [DisplayName("会议室预订信息")]
    [Table("MR_MeetingRoomBook")]
    public class MeetingRoomBookEntity : EntityBase, IAutoCreateTable
    {
        [PrimaryKey]
        public override string Id
        {
            get; set;
        }

        public string BookUserId { get; set; }


        public string MeetingRoomId { get; set; }

        public DateTime BeginTime { get; set; }

        public DateTime EndTime { get; set; }

        public DateTime CreateTime { get; set; }

        public string Title { get; set; }

        public string Description { get; set; }
    }
}
