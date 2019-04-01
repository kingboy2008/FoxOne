using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using FoxOne.Business;
using FoxOne.Core;
namespace FoxOne.Web
{
    /// <summary>
    /// 工作流显示视图对象
    /// </summary>
    public class WorkItemVO
    {
        /// <summary>
        /// 用户头像
        /// </summary>
        public string Avatar
        {
            get
            {
                string avatar = string.Empty;
                if (CreatorUserId.IsNullOrEmpty())
                {
                    avatar = "/images/end.png";
                }
                else
                {
                    var user = DBContext<IUser>.Instance.FirstOrDefault(u => u.Id == CreatorUserId);
                    if (user != null)
                    {
                        avatar = user.Avatar;
                        if (avatar.IsNullOrEmpty())
                        {
                            avatar = "/images/" + user.Sex + ".png";
                        }
                    }
                    else
                    {
                        throw new FoxOneException("User_Not_Found", CreatorUserId);
                    }
                }
                return avatar;
            }
        }

        /// <summary>
        /// 参与者ID
        /// </summary>
        public string CreatorUserId { get; set; }

        /// <summary>
        /// 工作项ID
        /// </summary>
        public int ItemId { get; set; }

        /// <summary>
        /// 参与者名称
        /// </summary>
        public string Creator { get; set; }

        /// <summary>
        /// 步骤名称
        /// </summary>
        public string ActivityName { get; set; }

        /// <summary>
        /// 意见内容
        /// </summary>
        public string Opinion { get; set; }

        /// <summary>
        /// 显示时间
        /// </summary>
        public string ShowTime { get; set; }

        /// <summary>
        /// 状态类型：receive,read,finish
        /// </summary>
        public string Status { get; set; }

        /// <summary>
        /// 状态
        /// </summary>
        public string StatusText { get; set; }
    }
}