using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Http;
using FoxOne.Business;
using FoxOne.Business.DDSDK;
using FoxOne.Data;
using FoxOne.Core;
using FoxOne.Business.DDSDK.Entity;

namespace FoxOne._3VJ.Controller
{
    /// <summary>
    /// 钉钉相关API
    /// </summary>
    public class DingDingController : ApiController
    {
        /// <summary>
        /// 发送钉钉消息
        /// </summary>
        /// <param name="message">消息参数</param>
        /// <returns>true则为发送成功</returns>
        [HttpPost]
        public bool SendMessage(DingDingMessage message)
        {
            if (message==null || message.Message.IsNullOrEmpty())
            {
                throw new FoxOneException("Parameter_Not_Null", "message");
            }
            if (message.Appid == 0)
            {
                throw new FoxOneException("Parameter_Not_Null", "appid");
            }
            if (message.UserList.IsNullOrEmpty() && message.DeptList.IsNullOrEmpty())
            {
                throw new FoxOneException("Atleast_Need_One_Of_UserList_Or_DeptList ", "UserList or DeptList");
            }
            if (Dao.Get().Query<SSOSystem>().Count(o => o.AppId == message.Appid) == 0)
            {
                throw new FoxOneException("System_Not_Found", message.Appid.ToString());
            }
            DDHelper.SendDDMessage(message.Appid, message.Message, message.UserList, message.DeptList, false);
            return true;
        }

        /// <summary>
        /// 获取钉钉的SignPackage
        /// </summary>
        /// <param name="appid">OA颁发给应用的唯一APPID</param>
        /// <param name="url">在钉钉注册应用时填写的主页地址</param>
        /// <returns></returns>
        [HttpGet]
        public SignPackage FetchSignPackage(int appid, string url)
        {
            if (url.IsNullOrEmpty())
            {
                throw new FoxOneException("Parameter_Not_Null", "url");
            }
            if (appid == 0)
            {
                throw new FoxOneException("Parameter_Not_Null", "appid");
            }
            return DDHelper.FetchSignPackage(appid, url);
        }
    }

    /// <summary>
    /// 钉钉消息
    /// </summary>
    public class DingDingMessage
    {
        /// <summary>
        /// OA颁发给应用的唯一APPID
        /// </summary>
        public int Appid { get; set; }

        /// <summary>
        /// 消息内容
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// 接收消息的用户的ID，多个用逗号隔开
        /// </summary>
        public string UserList { get; set; }

        /// <summary>
        /// 接收消息的部门ID，多个用逗号隔开
        /// </summary>
        public string DeptList { get; set; }


    }
}
