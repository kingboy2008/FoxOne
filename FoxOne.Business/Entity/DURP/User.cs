using System;
using System.Collections.Generic;
using System.Security.Principal;
using FoxOne.Core;
using System.Linq;
using FoxOne.Data.Attributes;
using System.ComponentModel;
using System.Web.Script.Serialization;
using System.Xml.Serialization;
namespace FoxOne.Business
{
    [Category("系统管理")]
    [DisplayName("用户信息")]
    [Table("SYS_User")]
    [Serializable]
    public class User : DURPBase, IUser, IAutoCreateTable
    {
        [DisplayName("账号")]
        public virtual string LoginId
        {
            get;
            set;
        }

        [Column(Showable=false)]
        public virtual string Password { get; set; }

        [TracePropertyChange]
        [DisplayName("所属部门")]
        public virtual string DepartmentId { get; set; }

        [ScriptIgnore]
        [XmlIgnore]
        public virtual IEnumerable<IRole> Roles
        {
            get 
            {
                var userRole = DBContext<IUserRole>.Instance.Where(o => o.UserId.IsNotNullOrEmpty() && o.UserId.Equals(Id, StringComparison.OrdinalIgnoreCase)).ToList().Select(o => o.RoleId);
                return DBContext<IRole>.Instance.Where(o => userRole.Contains(o.Id, StringComparer.OrdinalIgnoreCase));
            }
        }

        [ScriptIgnore]
        [XmlIgnore]
        public IDepartment Department
        {
            get 
            {
                return DBContext<IDepartment>.Instance.Get(DepartmentId);
            }
        }


        [DisplayName("手机")]
        [Column(Length="20")]
        public string MobilePhone
        {
            get;
            set;
        }


        [Column(Length="20")]
        public string QQ
        {
            get;
            set;
        }

        [DisplayName("邮箱")]
        [Column(Length = "100")]
        public string Mail { get; set; }

        [DisplayName("身份证")]
        [Column(Length = "20")]
        public string Identity { get; set; }


        [DisplayName("出生日期")]
        public DateTime Birthdate
        {
            get;
            set;
        }

        [DisplayName("性别")]
        [Column(Length="10")]
        public string Sex
        {
            get;
            set;
        }

        [DisplayName("头像")]
        [Column(Length = "200")]
        public string Avatar
        {
            get;
            set;
        }

        [DisplayName("工号")]
        [Column(Length = "20")]
        public string WorkNumber
        {
            get;
            set;
        }

        /// <summary>
        /// 薪资
        /// </summary>
        [DisplayName("薪资")]
        [Column(DataType ="int")]
        public int Salary { get; set; }
    }

    [DisplayName("用户外部授权信息")]
    [Table("SYS_UserClaim")]
    public class UserClaim:EntityBase,IAutoCreateTable
    {
        [PrimaryKey]
        public override string Id
        {
            get;set;
        }
        /// <summary>
        /// 用户Id
        /// </summary>
        public string UserId { get; set; }


        public string LoginId { get; set; }

        /// <summary>
        /// 用户认证类型
        /// </summary>
        public string Tag { get; set; }

        public string OpenId { get; set; }

        public string UnionId { get; set; }

        public string Token { get; set; }

        public string RefreshKey { get; set; }

    }

    [Table("SYS_UserValidCode")]
    public class UserValid:EntityBase,IAutoCreateTable
    {
        [PrimaryKey]
        public override string Id
        {
            get;set;
        }

        [Column(DataType ="varchar",Length ="15")]
        public string Phone { get; set; }

        [Column(DataType = "varchar", Length = "6")]
        public string ValidCode { get; set; }

        public DateTime ExpiredTime { get; set; }


    }
}