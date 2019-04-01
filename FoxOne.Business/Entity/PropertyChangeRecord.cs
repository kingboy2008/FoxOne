using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FoxOne.Core;
using FoxOne.Data.Attributes;

namespace FoxOne.Business
{
    [DisplayName("属性变更记录")]
    [Table("SYS_PropertyChangeRecord")]
    public class PropertyChangeRecord:EntityBase, IAutoCreateTable
    {
        /// <summary>
        /// 数据标识列值
        /// </summary>
        [DisplayName("数据标识列值")]
        [Column(DataType = "varchar", Length = SysConfig.PKLength, Update = false)]
        public string PKValue { get; set; }

        /// <summary>
        /// 类型名称
        /// </summary>
        [DisplayName("类型名称")]
        [Column(DataType = "varchar", Length = "30")]
        public string TypeName { get; set; }

        /// <summary>
        /// 属性名称
        /// </summary>
        [DisplayName("属性名称")]
        [Column(DataType = "varchar", Length = "30")]
        public string PropertyName { get; set; }

        /// <summary>
        /// 属性中文名称
        /// </summary>
        [DisplayName("属性中文名称")]
        [Column(DataType = "varchar", Length = "30")]
        public string PropertyCNName { get; set; }

        /// <summary>
        /// 原值
        /// </summary>
        [DisplayName("原值")]
        [Column(DataType = "varchar", Length = "50")]
        public string OriginalValue { get; set; }

        /// <summary>
        /// 新值
        /// </summary>
        [DisplayName("新值")]
        [Column(DataType = "varchar", Length = "50")]
        public string NewValue { get; set; }

        /// <summary>
        /// 创建人
        /// </summary>
        [DisplayName("创建人")]
        [Column(DataType = "varchar", Length = SysConfig.PKLength, Update = false)]
        public string CreatorId { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        [DisplayName("创建时间")]
        [Column(Update = false)]
        public DateTime CreateTime { get; set; }
    }
}
