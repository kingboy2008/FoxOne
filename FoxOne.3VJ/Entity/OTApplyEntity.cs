using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using FoxOne.Business;
using FoxOne.Data.Attributes;

namespace FoxOne._3VJ.DataSource
{

    /// <summary>
    /// 加班/调休申请实体
    /// </summary>
    [DisplayName("加班/调休申请实体")]
    [Table("wf_form_otapply")]
    public class OTApplyEntity : EntityBase
    {
        /// <summary>
        /// 主键
        /// </summary>
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
        /// 加班/调休开始时间
        /// </summary>
        public DateTime OTBeginTime { get; set; }

        /// <summary>
        /// 加班/调休结束时间
        /// </summary>
        public DateTime OTEndTime { get; set; }

        /// <summary>
        /// 加班/调休小时数
        /// </summary>
        public decimal OTTimeLast { get; set; }

        /// <summary>
        /// 描述
        /// </summary>
        public string OTDescription { get; set; }

        /// <summary>
        /// 如果是调休，则表示关联的加班申请
        /// </summary>
        public string RelateId { get; set; }

        /// <summary>
        /// 类型：1是加班，2是调休，3是上班补打，4是下班补打，5是特殊考勤
        /// </summary>
        public int OTType { get; set; }

        /// <summary>
        /// 出差目的地
        /// </summary>
        public string Desctination { get; set; }
    }
}
