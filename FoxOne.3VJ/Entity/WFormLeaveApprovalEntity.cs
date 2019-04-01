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
    /// 请假申请信息
    /// </summary>
    [DisplayName("请假申请信息")]
    [Table("WF_Form_LeaveApproval")]
    public class WFormLeaveApprovalEntity : EntityBase, IAutoCreateTable
    {
        [PrimaryKey]
        public override string Id
        {
            get; set;
        }

        /// <summary>
        /// 请假人
        /// </summary>
        [Column(DataType = "varchar", Length = "50")]
        public string CreatorId { get; set; }

        /// <summary>
        /// 所属部门
        /// </summary>
        [Column(DataType = "varchar", Length = "50")]
        public string CreatorDeptId { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreateTime { get; set; }

        /// <summary>
        /// 请假开始时间
        /// </summary>
        public DateTime BeginTime { get; set; }

        /// <summary>
        /// 请假结束时间
        /// </summary>
        public DateTime EndTime { get; set; }

        /// <summary>
        /// 请假理由
        /// </summary>
        [Column(DataType = "varchar", Length = "200")]
        public string Reason { get; set; }

        /// <summary>
        /// 请假类型
        /// </summary>
        [Column(DataType = "varchar", Length = "10")]
        public string Type { get; set; }

        /// <summary>
        /// 请假天数
        /// </summary>
        public decimal LeaveDays { get; set; }
    }
}
