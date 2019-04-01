using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using FoxOne.Business;
using FoxOne.Business.Environment;
using FoxOne.Business.Security;
using FoxOne.Core;
using FoxOne.Data.Attributes;

namespace FoxOne._3VJ
{
    [DisplayName("花名册信息")]
    [Table("SYS_UserInfo")]
    public class RosterEntity : EntityBase, IAutoCreateTable
    {
        /// <summary>
        /// 主键
        /// </summary>
        [PrimaryKey]
        [DisplayName("工号")]
        public override string Id
        {
            get; set;
        }

        /// <summary>
        /// 部门
        /// </summary>
        [XmlIgnore]
        [Column(IsDataField = false)]
        [DisplayName("部门")]
        public string Department
        {
            get
            {
                if (User != null)
                {
                    return User.Department.Name;
                }
                return string.Empty;
            }
        }

        /// <summary>
        /// 姓名
        /// </summary>
        [XmlIgnore]
        [Column(IsDataField = false)]
        [DisplayName("姓名")]
        public string Name
        {
            get
            {
                if (User != null)
                {
                    return User.Name;
                }
                return string.Empty;
            }
        }

        /// <summary>
        /// 卡通名
        /// </summary>
        [Column(Length = "20")]
        [DisplayName("卡通名")]
        public string Aliases { get; set; }

        /// <summary>
        /// 职位
        /// </summary>
        [Column(Length = "50")]
        [DisplayName("职位")]
        public string Position { get; set; }

        /// <summary>
        /// 性别
        /// </summary>
        [XmlIgnore]
        [Column(IsDataField = false)]
        [DisplayName("性别")]
        public string Sex
        {
            get
            {
                if (User != null)
                {
                    return User.Sex;
                }
                return string.Empty;
            }
        }

        /// <summary>
        /// 民族
        /// </summary>
        [Column(Length = "20")]
        [DisplayName("民族")]
        public string National { get; set; }

        /// <summary>
        /// 出生日期
        /// </summary>
        [XmlIgnore]
        [DisplayName("出生日期")]
        [Column(IsDataField = false)]
        public DateTime Birthdate
        {
            get
            {
                if (User != null)
                {
                    return User.Birthdate;
                }
                return DateTime.MinValue;
            }
        }

        /// <summary>
        /// 户口所在地
        /// </summary>
        [DisplayName("户口所在地")]
        [Column(Length = "200")]
        public string Address { get; set; }

        /// <summary>
        /// 最高学历(可枚举)
        /// </summary>
        [DisplayName("最高学历")]
        [Column(Length = "10")]
        public string Degree { get; set; }

        /// <summary>
        /// 毕业学校
        /// </summary>
        [DisplayName("毕业学校")]
        [Column(Length = "50")]
        public string School { get; set; }

        /// <summary>
        /// 专业
        /// </summary>
        [DisplayName("专业")]
        [Column(Length = "100")]
        public string Majors { get; set; }

        /// <summary>
        /// 身份证号码
        /// </summary>
        [XmlIgnore]
        [Column(IsDataField = false)]
        [DisplayName("身份证号码")]
        public string Identity
        {
            get
            {
                if (User != null)
                {
                    return User.Identity;
                }
                return string.Empty;
            }
        }
        /// <summary>
        /// 身份证地址
        /// </summary>
        [DisplayName("身份证地址")]
        [Column(Length = "200")]
        public string IdentityAddress { get; set; }

        /// <summary>
        /// 紧急联系人姓名
        /// </summary>
        [DisplayName("紧急联系人姓名")]
        [Column(Length = "20")]
        public string EmergencyContact { get; set; }

        /// <summary>
        /// （紧急联系人与员工关系）与员工的关系
        /// </summary>
        [DisplayName("与员工的关系")]
        [Column(Length = "20")]
        public string EmergencyRel { get; set; }

        /// <summary>
        /// 紧急联系人联系方式
        /// </summary>
        [DisplayName("紧急联系人联系方式")]
        [Column(Length = "100")]
        public string EmergencyPhone { get; set; }


        /// <summary>
        /// 生日月份
        /// </summary>
        [XmlIgnore]
        [Column(IsDataField = false)]
        [DisplayName("生日月份")]
        public int BirthMonth
        {
            get
            {
                if (User != null)
                {
                    return User.Birthdate.Month;
                }
                return 0;
            }
        }

        /// <summary>
        /// 户口性质(可枚举)
        /// </summary>
        [DisplayName("户口性质")]
        [Column(Length = "200")]
        public string HouseholdType { get; set; }

        /// <summary>
        /// 婚姻状态(可枚举)
        /// </summary>
        [DisplayName("婚姻状态")]
        public string Marital { get; set; }

        /// <summary>
        /// 生育情况
        /// </summary>
        [DisplayName("生育情况")]
        public string BirthType { get; set; }

        /// <summary>
        /// 入职时间
        /// </summary>
        [DisplayName("入职时间")]
        public DateTime EntryTime { get; set; }

        /// <summary>
        /// 转正日期
        /// </summary>
        [DisplayName("转正日期")]
        public DateTime ObtainmentDTime { get; set; }

        /// <summary>
        /// 手机
        /// </summary>
        [XmlIgnore]
        [Column(IsDataField = false)]
        [DisplayName("手机")]
        public string MobilePhone
        {
            get
            {
                if (User != null)
                {
                    return User.MobilePhone;
                }
                return string.Empty;
            }
        }

        /// <summary>
        /// QQ
        /// </summary>
        [XmlIgnore]
        [Column(IsDataField = false)]
        [DisplayName("QQ")]
        public string QQ
        {
            get
            {
                if (User != null)
                {
                    return User.QQ;
                }
                return string.Empty;
            }
        }

        /// <summary>
        /// 邮箱
        /// </summary>
        [XmlIgnore]
        [Column(IsDataField = false)]
        [DisplayName("企业邮箱")]
        public string Mail
        {
            get
            {
                if (User != null)
                {
                    return User.Mail;
                }
                return string.Empty;
            }
        }

        /// <summary>
        /// 合同编号
        /// </summary>
        [DisplayName("合同编号")]
        [Column(Length = "50")]
        public string ContractNo { get; set; }

        /// <summary>
        /// 合同开始时间
        /// </summary>
        [DisplayName("合同开始时间")]
        public DateTime ContractBeginTime { get; set; }

        /// <summary>
        /// 合同到期
        /// </summary>
        [DisplayName("合同到期")]
        public DateTime ContractFinishTime { get; set; }

        /// <summary>
        /// 银行卡号
        /// </summary>
        [DisplayName("银行卡号")]
        [Column(Length = "50")]
        public string BankCardNum { get; set; }

        /// <summary>
        /// 参保日期
        /// </summary>
        [DisplayName("参保日期")]
        public DateTime InsuranceTime { get; set; }

        /// <summary>
        /// 年龄
        /// </summary>
        [XmlIgnore]
        [Column(IsDataField = false)]
        [DisplayName("年龄")]
        public int Age
        {
            get
            {
                if (User != null)
                {
                    return DateTime.Now.Year - Birthdate.Year;
                }
                return 0;
            }
        }

        /// <summary>
        /// 入职时长
        /// </summary>
        [XmlIgnore]
        [Column(IsDataField = false)]
        [DisplayName("入职时长")]
        public decimal EntryLength
        {
            get
            {
                if (LeaveTime != DateTime.MinValue)
                {
                    return ((LeaveTime - EntryTime).TotalDays / 365).ConvertTo<decimal>();
                }
                else
                {
                    return ((DateTime.Now - EntryTime).TotalDays / 365).ConvertTo<decimal>();
                }

            }
        }

        /// <summary>
        /// 实习开始日期
        /// </summary>
        [DisplayName("实习开始日期")]
        public DateTime PracticeBeginTime { get; set; }

        /// <summary>
        /// 实习结束日期
        /// </summary>
        [DisplayName("实习结束日期")]
        public DateTime PracticeEndTime { get; set; }

        /// <summary>
        /// 参加工作时间
        /// </summary>
        [DisplayName("参加工作时间")]
        public DateTime WorkingTime { get; set; }

        /// <summary>
        /// 离职类型(可枚举)
        /// </summary>
        [DisplayName("离职类型")]
        public string LeaveType { get; set; }

        /// <summary>
        /// 离职日期
        /// </summary>
        [DisplayName("离职日期")]
        public DateTime LeaveTime { get; set; }

        /// <summary>
        /// 招聘渠道
        /// </summary>
        [DisplayName("招聘渠道")]
        public string RecruitmentType { get; set; }

        /// <summary>
        /// 渠道名称
        /// </summary>
        [DisplayName("渠道名称")]
        [Column(Length = "100")]
        public string RecruitmentName { get; set; }



        /// <summary>
        /// 身高
        /// </summary>
        [DisplayName("身高")]
        public int Height { get; set; }

        /// <summary>
        /// 体重
        /// </summary>
        [DisplayName("体重")]
        public int Weight { get; set; }

        /// <summary>
        /// 血型(可枚举)
        /// </summary>
        [DisplayName("血型")]
        public string Blood { get; set; }

        /// <summary>
        /// 祖籍
        /// </summary>
        [DisplayName("祖籍")]
        [Column(Length = "200")]
        public string Native { get; set; }

        /// <summary>
        /// 是否已登记的伤残人员
        /// </summary>
        [DisplayName("是否已登记的伤残人员")]
        public string IsDisable { get; set; }

        /// <summary>
        /// 社会保险IC编号
        /// </summary>
        [Column(Length = "20")]
        [DisplayName("社会保险IC编号")]
        public string SocialICNo { get; set; }

        /// <summary>
        /// 职位类别(可枚举)
        /// </summary>
        [DisplayName("职位类别")]
        public string JobType { get; set; }

        /// <summary>
        /// 职位名称
        /// </summary>
        [Column(Length = "50")]
        [DisplayName("职位名称")]
        public string JobName { get; set; }

        /// <summary>
        /// 职位级别(可枚举)
        /// </summary>
        [DisplayName("职位级别")]
        public string JobLevel { get; set; }

        /// <summary>
        /// 实习开始时间(试用开始时间)
        /// </summary>
        [DisplayName("实习开始时间")]
        public DateTime TryBeginTime { get; set; }

        /// <summary>
        /// 实习结束时间(试用结束时间)
        /// </summary>
        [DisplayName("实习结束时间")]
        public DateTime TryEndTime { get; set; }

        /// <summary>
        /// 转正日期
        /// </summary>
        [DisplayName("转正日期")]
        public DateTime PositiveTime { get; set; }

        /// <summary>
        /// 住宅电话号码
        /// </summary>
        [Column(Length = "20")]
        [DisplayName("住宅电话号码")]
        public string HousePhone { get; set; }

        /// <summary>
        /// 第一外语
        /// </summary>
        [DisplayName("第一外语")]
        public string FirstForeLang { get; set; }

        /// <summary>
        /// 第一外语级别
        /// </summary>
        [DisplayName("第一外语级别")]
        public string FirstForeLangLevel { get; set; }

        /// <summary>
        /// 第二外语
        /// </summary>
        [DisplayName("第二外语")]
        public string SecondForeLang { get; set; }

        /// <summary>
        /// 第二外语级别
        /// </summary>
        [DisplayName("第二外语级别")]
        public string SecondForeLangLevel { get; set; }

        /// <summary>
        /// 合同状态
        /// </summary>
        [DisplayName("合同状态")]
        public string ContractState { get; set; }

        /// <summary>
        /// 开户行名称
        /// </summary>
        [DisplayName("开户行名称")]
        public string BankName { get; set; }

        /// <summary>
        /// 开户行所在城市
        /// </summary>
        [DisplayName("开户行所在城市")]
        public string BankCity { get; set; }

        /// <summary>
        /// 开户行省份
        /// </summary>
        [DisplayName("开户行省份")]
        public string BankProvince { get; set; }

        /// <summary>
        /// 开放员工编辑
        /// </summary>
        [DisplayName("开放员工编辑")]
        public string UserEdit { get; set; }

        private IUser _user;

        [XmlIgnore]
        //[ScriptIgnore]
        [Column(IsDataField = false)]
        internal IUser User
        {
            get
            {
                if (_user == null && Id.IsNotNullOrEmpty())
                {
                    _user = DBContext<IUser>.Instance.FirstOrDefault(c => Id.Equals(c.WorkNumber, StringComparison.InvariantCultureIgnoreCase));
                }
                return _user;
            }
        }
        
    }

    [DisplayName("花名册数据源")]
    public class RosterDataSource : ListDataSourceBase,IFormService
        {

            private const string IN_SERVICE = "在职";

            public string UserId
            {
                get
                {
                    if (!Parameter.IsNullOrEmpty() && Parameter.ContainsKey("UserId"))
                    {
                        return Parameter["UserId"].ToString();
                    }
                    return string.Empty;
                }
            }

            public string LeaveType
            {
                get
                {
                    if (!Parameter.IsNullOrEmpty() && Parameter.ContainsKey("LeaveType"))
                    {
                        return Parameter["LeaveType"].ToString();
                    }
                    return string.Empty;
                }
            }

        public int Delete(string key)
        {
            //return DBContext<RosterEntity>.Instance
            return 1;
        }

        public IDictionary<string, object> Get(string key)
        {
             return DBContext<RosterEntity>.Instance.FirstOrDefault(c => c.Id.Equals(key)).ToDictionary();
        }

        public int Insert(IDictionary<string, object> data)
        {
            //DBContext<RosterEntity>.Instance.Add(data.ToEntity<RosterEntity>());
            var result= DBContext<RosterEntity>.Insert(data.ToEntity<RosterEntity>());
            return result ? 1 : 0;
        }

        public int Update(string key, IDictionary<string, object> data)
        {
            var result= DBContext<RosterEntity>.Update(data.ToEntity<RosterEntity>());
            return result ? 1 : 0;
        }

        protected override IEnumerable<IDictionary<string, object>> GetListInner()
            {
                IEnumerable<RosterEntity> datas = DBContext<RosterEntity>.Instance;
                var result = new List<Dictionary<string, object>>();

                //IEnumerable<IUser> userList = DBContext<IUser>.Instance.OrderBy(c =>
                //{
                //    if (c.WorkNumber.IsNotNullOrEmpty())
                //    {
                //        return c.WorkNumber.ConvertTo<int>();
                //    }
                //    return int.MaxValue;
                //});

                if (UserId.IsNotNullOrEmpty())
                {
                    datas = datas.Where(c => c.User.Id.Equals(UserId));
                    //userList =userList.Where(c => c.Id.Equals(UserId));
                }
                if (LeaveType.IsNotNullOrEmpty())
                {
                    if (LeaveType.Equals(IN_SERVICE))
                    {
                        datas = datas.Where(c => c.LeaveType.Equals(LeaveType));
                    }
                    else
                    {
                        datas = datas.Where(c => !c.LeaveType.Equals(IN_SERVICE));
                    }
                }
                var dic = datas.ToDictionary(c => c.Id);
                var ft = FastType.Get(typeof(RosterEntity));

                //foreach (var user in userList)
                //{
                //    var row = new Dictionary<string, object>();
                //    if (user.WorkNumber.IsNotNullOrEmpty())
                //    {
                //        if (dic.ContainsKey(user.WorkNumber))
                //        {
                //            var roster = dic[user.WorkNumber];
                //            foreach (var getter in ft.Getters)
                //            {
                //                row[getter.Info.GetDisplayName()] = getter.GetValue(roster);
                //            }
                //        }
                //    }
                //    row["工号"] = user.WorkNumber;
                //    row["姓名"] = user.Name;
                //    result.Add(row);
                //}
                foreach (var item in datas)
                {
                    var row = new Dictionary<string, object>();
                    foreach (var getter in ft.Getters)
                    {
                    //row[getter.Info.GetDisplayName()] = getter.GetValue(item);
                    row[getter.Name] = getter.GetValue(item);
                    }
                    result.Add(row);
                }

                return result;
            }
        }

    public class RosterProvider : IEnvironmentProvider
    {
        public string Prefix
        {
            get
            {
                return "Roster";
            }
        }

        public object Resolve(string name)
        {
            object value = DBContext<RosterEntity>.Instance.FirstOrDefault(c=>c.Id.Equals(Sec.User.WorkNumber));
            if (value == null)
            {
                return string.Empty;
            }
            FastType fastType = null;
            if (name.IndexOf(".") > 0)
            {
                string[] propAtt = name.Split('.');
                foreach (var item in propAtt)
                {
                    fastType = FastType.Get(value.GetType());
                    var getter = fastType.GetGetter(item);
                    if (getter != null)
                    {
                        value = getter.GetValue(value);
                    }
                    else
                    {
                        return name;
                    }
                }
                return value;
            }
            else
            {
                fastType = FastType.Get(value.GetType());
                var getter = fastType.GetGetter(name);
                if (getter != null)
                {
                    var res= getter.GetValue(value);
                    if (res == null)
                    {
                        return string.Empty;
                    }
                    return res;
                }
                else
                {
                    return name;
                }
            }
        }
    }
}
