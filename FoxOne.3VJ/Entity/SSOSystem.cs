using FoxOne.Business;
using FoxOne.Core;
using FoxOne.Data.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FoxOne._3VJ
{
    /// <summary>
    /// 单点登录系统信息
    /// </summary>
    [Table("sso_system")]
    public class SSOSystem : EntityBase, IAutoCreateTable
    {
        [PrimaryKey]
        [Column(DataType = "varchar", Length = SysConfig.PKLength)]
        public override string Id
        {
            get; set;
        }

        /// <summary>
        /// 系统名称
        /// </summary>
        [Column(Length = "20")]
        public string SystemName { get; set; }

        /// <summary>
        /// appid
        /// </summary>
        public int AppId { get; set; }

        /// <summary>
        /// appsecret
        /// </summary>
        [Column(Length = "32")]
        public string AppSecret { get; set; }

        /// <summary>
        /// 登录页地址
        /// </summary>
        public string LogOnUrl { get; set; }

        /// <summary>
        /// 主页地址
        /// </summary>
        public string HomeUrl { get; set; }

        /// <summary>
        /// 应用ID
        /// </summary>
        public int AgentId { get; set; }

        /// <summary>
        /// logo
        /// </summary>
        public string Logo { get; set; }

        /// <summary>
        /// 创建者
        /// </summary>
        [Column(DataType = "varchar", Length = SysConfig.PKLength)]
        public string CreatorId { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreateTime { get; set; }

        /// <summary>
        /// 管理员ID
        /// </summary>
        [Column(DataType = "varchar", Length = SysConfig.PKLength)]
        public string ManagerUserId { get; set; }

        /// <summary>
        /// 访问票据
        /// </summary>
        [Column(DataType = "varchar", Length = "150")]
        public string Token { get; set; }

    }

    [Table("sso_systemtoken")]
    public class SSOSystemToken : EntityBase, IAutoCreateTable
    {
        [PrimaryKey]
        [Column(DataType = "varchar", Length = SysConfig.PKLength)]
        public override string Id
        {
            get; set;
        }

        /// <summary>
        /// appid
        /// </summary>
        public int AppId { get; set; }

        /// <summary>
        /// 是否已使用
        /// </summary>
        public bool IsUse { get; set; }

        /// <summary>
        /// 访问票据
        /// </summary>
        [Column(DataType = "varchar", Length = "200")]
        public string Token { get; set; }

    }
}
