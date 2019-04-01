using FoxOne.Data.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FoxOne.Core;
namespace FoxOne.Business.DDSDK.Entity
{

    public class DDUserCreateInfo : ResultPackage
    {
        /// <summary>
        /// 员工唯一标识ID（不可修改），企业内必须唯一。长度为1~64个字符，如果不传，服务器将自动生成一个userid
        /// </summary>
        [PrimaryKey]
        public string userid { get; set; }

        /// <summary>
        /// 成员名称。长度为1~64个字符
        /// </summary>
        public string name { get; set; }

        /// <summary>
        /// 分机号，长度为0~50个字符，企业内必须唯一不能重复
        /// </summary>
        public string tel { get; set; }

        /// <summary>
        /// 办公地点，长度为0~50个字符
        /// </summary>
        public string workPlace { get; set; }

        /// <summary>
        /// 备注，长度为0~1000个字符
        /// </summary>
        public string remark { get; set; }

        /// <summary>
        /// 手机号码，企业内必须唯一
        /// </summary>
        public string mobile { get; set; }

        /// <summary>
        /// 邮箱。长度为0~64个字符。企业内必须唯一
        /// </summary>
        public string email { get; set; }

        /// <summary>
        /// 员工工号。对应显示到OA后台和客户端个人资料的工号栏目。长度为0~64个字符
        /// </summary>
        public string jobnumber { get; set; }

        /// <summary>
        /// 数组类型，数组里面值为整型，成员所属部门id列表
        /// </summary>
        [Column(IsDataField = false)]
        public int[] department { get; set; }

        private string _departmentId;

        /// <summary>
        /// 部门Id
        /// </summary>
        public string departmentId
        {
            get
            {
                if (_departmentId.IsNullOrEmpty())
                {
                    if (department.Length > 0)
                    {
                        return department[0].ToString();
                    }
                    return "";
                }
                return _departmentId;
            }
            set
            {
                _departmentId = value;
            }
        }

        /// <summary>
        /// 职位信息。长度为0~64个字符
        /// </summary>
        public string position { get; set; }
    }

    [Table("dd_user")]
    public class DDUserInfo : DDUserCreateInfo
    {
        /// <summary>
        /// 表示该用户是否激活了钉钉
        /// </summary>
        public bool active { get; set; }

        /// <summary>
        /// 是否是企业的管理员, true表示是, false表示不是
        /// </summary>
        public bool isAdmin { get; set; }

        /// <summary>
        /// 是否为企业的老板, true表示是, false表示不是 （不能通过接口设置,可以通过OA后台设置）
        /// </summary>
        public bool isBoss { get; set; }

        /// <summary>
        /// 钉钉ID（不可修改）
        /// </summary>
        public string dingId { get; set; }

        public string unionid { get; set; }

        /// <summary>
        /// 是否隐藏号码, true表示是, false表示不是
        /// </summary>
        public bool isHide { get; set; }

        /// <summary>
        /// 头像url
        /// </summary>
        public string avatar { get; set; }

        public string deviceId { get; set; }

        public bool is_sys { get; set; }

        public int sys_level { get; set; }
    }

    public class DDUserPackage : ResultPackage
    {
        public IList<DDUserInfo> userlist { get; set; }
    }
}
