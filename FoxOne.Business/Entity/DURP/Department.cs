using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FoxOne.Core;
using FoxOne.Data.Attributes;
using System.ComponentModel;
using System.Web.Script.Serialization;
using System.Xml.Serialization;
namespace FoxOne.Business
{
    [Category("系统管理")]
    [DisplayName("组织信息")]
    [Table("SYS_Department")]
    public class Department : DURPBase, IDepartment, IAutoCreateTable
    {
        [Column(Showable = false)]
        public string ParentId
        {
            get;
            set;
        }

        [DisplayName("级别")]
        [Column(Showable = false)]
        public int Level
        {
            get;
            set;
        }

        [XmlIgnore]
        [ScriptIgnore]
        public IDepartment Parent
        {
            get
            {
                return DBContext<IDepartment>.Instance.Get(ParentId);
            }
        }

        [XmlIgnore]
        [ScriptIgnore]
        public IEnumerable<IDepartment> Childrens
        {
            get
            {
                return DBContext<IDepartment>.Instance.Where(o => o.ParentId.IsNotNullOrEmpty() && o.ParentId.Equals(Id, StringComparison.OrdinalIgnoreCase));
            }
        }

        [XmlIgnore]
        [ScriptIgnore]
        public IEnumerable<IUser> Member
        {
            get
            {
                return DBContext<IUser>.Instance.Where(o => o.DepartmentId.IsNotNullOrEmpty() && o.DepartmentId.Equals(Id, StringComparison.OrdinalIgnoreCase));
            }
        }

        [XmlIgnore]
        [ScriptIgnore]
        public IEnumerable<IRole> Roles
        {
            get
            {
                return DBContext<IRole>.Instance.Where(o => o.DepartmentId.IsNotNullOrEmpty() && o.DepartmentId.Equals(Id, StringComparison.OrdinalIgnoreCase));
            }
        }

        [Column(Showable = false)]
        public string WBS
        {
            get;
            set;
        }
    }

    [DisplayName("部门角色转换器")]
    public class DepartmentConverter : ColumnConverterBase
    {
        public override object Converter(object value)
        {
            StringBuilder sb = new StringBuilder();
            var dept = DBContext<IDepartment>.Get(value.ToString());
            var roles = dept.Roles;
            sb.Append($"<p class=\"dept-role-add\" deptname=\"{dept.Name}\" deptid=\"{value}\">新增角色</p>");
            if (!roles.IsNullOrEmpty())
            {
                foreach (var role in roles)
                {
                    sb.Append($"<p class=\"dept-role\">{role.RoleType.Name}：{(role.Members.IsNullOrEmpty() ? string.Empty : string.Join("，", role.Members.Select(o =>$"<span roleid=\"{role.Id}\" userid=\"{o.Id}\" class=\"role-user\">{o.Name}</span>")))}<a deptname=\"{dept.Name}\" rolename=\"{role.RoleType.Name}\" roleid=\"{role.Id}\">新增角色人员</a></p>");
                }
            }
            if(!dept.Member.IsNullOrEmpty())
            {
                sb.Append($"<p class=\"dept-role\">部门成员：{string.Join("，", dept.Member.Select(o => $"<span  roleid=\"\" userid=\"\" class=\"role-user\">{o.Name}</span>"))}</p>");
            }
            return sb.ToString();
        }
    }
}
