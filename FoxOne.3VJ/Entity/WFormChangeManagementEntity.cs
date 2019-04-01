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
    /// 变更管理信息
    /// </summary>
    [DisplayName("变更管理表单")]
    [Table("WF_Form_ChangeManagement")]
    public class WFormChangeManagementEntity : EntityBase, IAutoCreateTable
    {
        [PrimaryKey]
        public override string Id
        {
            get; set;
        }

        [Column(DataType = "varchar", Length = "100")]
        public string Title { get; set; }

        /// <summary>
        /// 变更编号
        /// </summary>
        [Column(DataType = "varchar", Length = "30")]
        public string Code { get; set; }

        [Column(DataType = "text")]
        public string Reason { get; set; }

        [Column(DataType = "text")]
        public string ChangeDescription { get; set; }

        [Column(DataType = "varchar", Length = "50")]
        public string CreatorId { get; set; }

        public DateTime CreateTime { get; set; }

        [Column(DataType = "text")]
        public string ChangePlan { get; set; }

        [Column(DataType = "text")]
        public string RollbackPlan { get; set; }

        public DateTime BeginTime { get; set; }

        public DateTime EndTime { get; set; }

        public string ResponsibilityUserId { get; set; }

        [Column(DataType = "text")]
        public string CheckList { get; set; }

        [Column(DataType = "varchar", Length = "10")]
        public string RiskLevel { get; set; }

        [Column(DataType = "varchar", Length = "50")]
        public string RiskAssessor { get; set; }

    }
}
