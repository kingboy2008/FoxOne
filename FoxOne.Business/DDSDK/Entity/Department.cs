using FoxOne.Data.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FoxOne.Business.DDSDK.Entity
{
    [Table("dd_department")]
    public class DDDepartmentInfo:ResultPackage
    {
        
        /// <summary>
        /// 部门id
        /// </summary>
        [PrimaryKey]
        public int id { get; set; }

        /// <summary>
        /// 部门名称。长度限制为1~64个字符。不允许包含字符‘-’‘，’以及‘,’。
        /// </summary>
        public string name { get; set; }

        /// <summary>
        /// 父部门id，根部门为1
        /// </summary>
        public int parentid { get; set; }

        /// <summary>
        /// 在父部门中的次序值。order值小的排序靠前
        /// </summary>
        public int order { get; set; }

        /// <summary>
        /// 是否同步创建一个关联此部门的企业群, true表示是, false表示不是
        /// </summary>
        public bool createDeptGroup { get; set; }

        /// <summary>
        /// 当群已经创建后，是否有新人加入部门会自动加入该群, true表示是, false表示不是
        /// </summary>
        public bool autoAddUser { get; set; }
    }

    public class DDDepartmentPackage : ResultPackage
    {
        public List<DDDepartmentInfo> department { get; set; }
    }
}
