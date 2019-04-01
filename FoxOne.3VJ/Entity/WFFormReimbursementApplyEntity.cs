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
    /// 费用报销单
    /// </summary>
    [DisplayName("费用报销单")]
    [Table("WF_Form_ReimbursementApply")]
    public class WFFormReimbursementApplyEntity : EntityBase, IAutoCreateTable
    {
        [PrimaryKey]
        public override string Id
        {
            get; set;
        }

        [Column(DataType = "varchar", Length = "20")]
        public string Code { get; set; }

        [Column(DataType = "varchar", Length = "50")]
        public string CreatorId { get; set; }

        [Column(DataType = "varchar", Length = "50")]
        public string CreatorDeptId { get; set; }

        public decimal TotalPrice { get; set; }

        public DateTime CreateTime { get; set; }

        [Column(DataType = "varchar", Length = "200")]
        public string Description { get; set; }
    }

    /// <summary>
    /// 费用报销单明细
    /// </summary>
    [DisplayName("费用报销单明细")]
    [Table("WF_Form_ReimbursementApplyDetail")]
    public class WFFormReimbursementApplyDetailEntity : EntityBase, IAutoCreateTable
    {
        [PrimaryKey]
        public override string Id
        {
            get; set;
        }

        [Column(DataType = "varchar", Length = "50")]
        public string ReimbursementApplyId { get; set; }

        [Column(DataType = "varchar", Length = "100")]
        public string ItemName { get; set; }

        public decimal ItemPrice { get; set; }

        [Column(DataType = "varchar", Length = "200")]
        public string ItemDescription { get; set; }
    }
}
