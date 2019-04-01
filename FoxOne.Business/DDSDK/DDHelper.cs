using FoxOne.Business.DDSDK.Entity;
using FoxOne.Core;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using DingTalk.Api;
using DingTalk.Api.Request;
using DingTalk.Api.Response;
using System.Web.Caching;

namespace FoxOne.Business.DDSDK
{
    public static class DDHelper
    {

        public static object AccessToken = new object();

        /// <summary>  
        ///更新票据  
        /// </summary>  
        public static string GetAccessToken()
        {
            string accessToken = CacheHelper.GetValue(Keys.CACHE_ACCESS_TOKEN) as string;
            if (accessToken.IsNullOrEmpty())
            {
                lock (AccessToken)
                {
                     accessToken = CacheHelper.GetValue(Keys.CACHE_ACCESS_TOKEN) as string;
                    if (accessToken.IsNullOrEmpty())
                    {
                        string CorpID = FetchAppID();
                        string CorpSecret = FetchAppSecret();
                        string apiurl = $"{Urls.gettoken}?{Keys.corpid}={CorpID}&{Keys.corpsecret}={CorpSecret}";
                        TokenResult tokenResult = JSONHelper.Deserialize<TokenResult>(HttpHelper.Get(apiurl));
                        if (tokenResult.IsOK())
                        {
                            accessToken = tokenResult.Access_token;
                            CacheHelper.SetValue(Keys.CACHE_ACCESS_TOKEN, accessToken, DateTime.Now.AddSeconds(Keys.CACHE_TIME), Cache.NoSlidingExpiration);
                        }
                        else
                        {
                            throw new Exception("取access_token异常："+tokenResult.ErrMsg);
                        }
                    }
                }
            }
            return accessToken;
        }

        public static SignPackage FetchSignPackage(string url)
        {
            return FetchSignPackage(FetchAgentID().ConvertTo<int>(), url);
        }

        /// <summary>  
        /// 获取签名包  
        /// </summary>  
        /// <param name="url"></param>  
        /// <returns></returns>  
        public static SignPackage FetchSignPackage(int agentId, string url)
        {
            var jsticket = FetchJSTicket();
            if (jsticket == null)
            {
                return null;
            }
            string timestamp = ConvertToUnixTimeStamp(DateTime.Now).ToString();
            string nonceStr = CreateNonceStr();
            // 这里参数的顺序要按照 key 值 ASCII 码升序排序   
            string rawstring = $"{Keys.jsapi_ticket}=" + jsticket
                             + $"&{Keys.noncestr}=" + nonceStr
                             + $"&{Keys.timestamp}=" + timestamp
                             + $"&{Keys.url}=" + url;
            string signature = Sha1Hex(rawstring).ToLower();

            var signPackage = new SignPackage()
            {
                agentId = agentId,
                corpId = FetchAppID(),//取配置文件中的coprId，可依据实际配置而作调整  
                timeStamp = timestamp,
                nonceStr = nonceStr,
                signature = signature,
                url = url,
                //rawstring = rawstring,
                jsticket = jsticket
            };
            return signPackage;
        }

        /// <summary>  
        /// 获取JS票据  
        /// </summary>  
        /// <param name="url"></param>  
        /// <returns></returns>  
        public static string FetchJSTicket()
        {
            string jsTicket = CacheHelper.GetValue(Keys.CACHE_JS_TICKET_KEY) as string;
            if(jsTicket.IsNullOrEmpty())
            {
                lock (AccessToken)
                {
                    jsTicket = CacheHelper.GetValue(Keys.CACHE_JS_TICKET_KEY) as string;
                    if (jsTicket.IsNullOrEmpty())
                    {
                        string apiurl = $"{Urls.get_jsapi_ticket}?{Keys.access_token}={GetAccessToken()}";
                        var result = JSONHelper.Deserialize<JSTicket>(HttpHelper.Get(apiurl));
                        if(result.IsOK())
                        {
                            jsTicket = result.ticket;
                            CacheHelper.SetValue(Keys.CACHE_JS_TICKET_KEY, jsTicket, DateTime.Now.AddSeconds(result.expires_in), Cache.NoSlidingExpiration);
                        }
                        else
                        {
                            throw new Exception("取js_ticket异常：" + result.ErrMsg);
                        }
                    }
                }
            }

            return jsTicket;
        }

        public static string GetUserInfo(string code)
        {
            string apiurl = $"{Urls.get_user_info}?{Keys.access_token}={GetAccessToken()}&{Keys.code}={code}";
            var result = JSONHelper.Deserialize<DDUserInfo>(HttpHelper.Get(apiurl));
            if (result.ErrCode == 0)
            {
                return result.userid;
            }
            else
            {
                throw new Exception(result.ErrMsg);
            }
        }

        public static string Sha1Hex(string value)
        {
            SHA1 algorithm = SHA1.Create();
            byte[] data = algorithm.ComputeHash(Encoding.UTF8.GetBytes(value));
            string sh1 = "";
            for (int i = 0; i < data.Length; i++)
            {
                sh1 += data[i].ToString("x2").ToUpperInvariant();
            }
            return sh1;
        }

        /// <summary>  
        /// 创建随机字符串  
        /// </summary>  
        /// <returns></returns>  
        public static string CreateNonceStr()
        {
            int length = 16;
            string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            string str = "";
            Random rad = new Random();
            for (int i = 0; i < length; i++)
            {
                str += chars.Substring(rad.Next(0, chars.Length - 1), 1);
            }
            return str;
        }

        /// <summary>    
        /// 将DateTime时间格式转换为Unix时间戳格式    
        /// </summary>    
        /// <param name="time">时间</param>    
        /// <returns>double</returns>    
        public static int ConvertToUnixTimeStamp(DateTime time)
        {
            int intResult = 0;
            System.DateTime startTime = TimeZone.CurrentTimeZone.ToLocalTime(new System.DateTime(1970, 1, 1));
            intResult = Convert.ToInt32((time - startTime).TotalSeconds);
            return intResult;
        }

        /// <summary>  
        /// 获取CorpID  
        /// </summary>  
        /// <returns></returns>  
        public static String FetchCorpID()
        {
            return FetchValue("CorpID");
        }

        /// <summary>
        /// 获取AppID  
        /// </summary>
        /// <returns></returns>
        public static string FetchAppID()
        {
            return FetchValue("AppID");
        }

        /// <summary>  
        /// 获取AppSecret  
        /// </summary>  
        /// <returns></returns>  
        public static String FetchAppSecret()
        {
            return FetchValue("AppSecret");
        }

        private static String FetchValue(String key)
        {
            String value = ConfigurationManager.AppSettings[key];
            if (value == null)
            {
                throw new Exception($"{key} is null.请确认配置文件中已配置.");
            }
            return value;
        }

        public static string FetchAgentID()
        {
            return FetchValue("AgentID");
        }


        public static void SendDDMessage(long agentId, string message, string userCodeList = default(string), string deptCodeList = default(string), bool toAllUser = false)
        {
            IDingTalkClient client = new DefaultDingTalkClient("https://eco.taobao.com/router/rest");
            CorpMessageCorpconversationAsyncsendRequest req = new CorpMessageCorpconversationAsyncsendRequest();
            req.Msgtype = "text";
            req.AgentId = agentId;
            req.UseridList = userCodeList;
            req.DeptIdList = deptCodeList;
            req.ToAllUser = toAllUser;
            req.Msgcontent = $"{{\"content\":\"{message}\"}}";
            //req.Msgcontent = "{\"message_url\": \"http://dingtalk.com\",\"head\": {\"bgcolor\": \"FFBBBBBB\",\"text\": \"头部标题\"},\"body\": {\"title\": \"正文标题\",\"form\": [{\"key\": \"姓名:\",\"value\": \"张三\"},{\"key\": \"爱好:\",\"value\": \"打球、听音乐\"}],\"rich\": {\"num\": \"15.6\",\"unit\": \"元\"},\"content\": \"大段文本大段文本大段文本大段文本大段文本大段文本大段文本大段文本大段文本大段文本大段文本大段文本\",\"image\": \"@lADOADmaWMzazQKA\",\"file_count\": \"3\",\"author\": \"李四 \"}}";
            CorpMessageCorpconversationAsyncsendResponse rsp = client.Execute(req, GetAccessToken());
            if(rsp.IsError)
            {
                throw new FoxOneException(rsp.ErrMsg);
            }
            if (!rsp.Result.Success)
            {
                throw new FoxOneException(rsp.Result.DingOpenErrcode + "：" + rsp.Result.ErrorMsg);
            }
        }

        public static void SendDDLinkMessage(long agentId, string link, string title, string message, string userCodeList = default(string), string deptCodeList = default(string), bool toAllUser = false)
        {
            IDingTalkClient client = new DefaultDingTalkClient("https://eco.taobao.com/router/rest");
            CorpMessageCorpconversationAsyncsendRequest req = new CorpMessageCorpconversationAsyncsendRequest();
            req.Msgtype = "link";
            req.AgentId = agentId;
            req.UseridList = userCodeList;
            req.DeptIdList = deptCodeList;
            req.ToAllUser = toAllUser;
            req.Msgcontent = $"{{\"messageUrl\": \"{link}\", \"picUrl\":\"@lALOACZwe2Rk\",\"title\": \"{title}\",\"text\": \"{message}\"}}";
            //req.Msgcontent = "{\"message_url\": \"http://dingtalk.com\",\"head\": {\"bgcolor\": \"FFBBBBBB\",\"text\": \"头部标题\"},\"body\": {\"title\": \"正文标题\",\"form\": [{\"key\": \"姓名:\",\"value\": \"张三\"},{\"key\": \"爱好:\",\"value\": \"打球、听音乐\"}],\"rich\": {\"num\": \"15.6\",\"unit\": \"元\"},\"content\": \"大段文本大段文本大段文本大段文本大段文本大段文本大段文本大段文本大段文本大段文本大段文本大段文本\",\"image\": \"@lADOADmaWMzazQKA\",\"file_count\": \"3\",\"author\": \"李四 \"}}";
            CorpMessageCorpconversationAsyncsendResponse rsp = client.Execute(req, GetAccessToken());
            if (rsp.IsError)
            {
                throw new FoxOneException(rsp.ErrMsg);
            }
            if (!rsp.Result.Success)
            {
                throw new FoxOneException(rsp.Result.DingOpenErrcode + "：" + rsp.Result.ErrorMsg);
            }
        }

        public static void SendDDTextMessage(string message, bool toAllUser = false, string userList = default(string), string deptList = default(string))
        {
            if (!toAllUser)
            {
                if (userList.IsNotNullOrEmpty())
                {
                    string[] userIds = userList.Split(',');
                    userList = string.Join(",", DBContext<IUser>.Instance.Where(o => userIds.Contains(o.Id)).Select(o => o.Code).ToArray());
                }
                if (deptList.IsNotNullOrEmpty())
                {
                    string[] deptIds = deptList.Split(',');
                    deptList = string.Join(",", DBContext<IDepartment>.Instance.Where(o => deptIds.Contains(o.Id)).Select(o => o.Code).ToArray());
                }
            }
            SendDDMessage(FetchAgentID().ConvertTo<long>(), message, userList, deptList, toAllUser);
        }

        public static void SendDDLinkMessage(string link, string title, string message, bool toAllUser = false, string userList = default(string), string deptList = default(string))
        {
            if (!toAllUser)
            {
                if (userList.IsNotNullOrEmpty())
                {
                    string[] userIds = userList.Split(',');
                    userList = string.Join(",", DBContext<IUser>.Instance.Where(o => userIds.Contains(o.Id)).Select(o => o.Code).ToArray());
                }
                if (deptList.IsNotNullOrEmpty())
                {
                    string[] deptIds = deptList.Split(',');
                    deptList = string.Join(",", DBContext<IDepartment>.Instance.Where(o => deptIds.Contains(o.Id)).Select(o => o.Code).ToArray());
                }
            }
            SendDDLinkMessage(FetchAgentID().ConvertTo<long>(), link, title, message, userList, deptList, toAllUser);
        }

        /// <summary>
        /// 获取钉钉部门Id
        /// </summary>
        public static int GetDDDepartId()
        {
            var date = DateTime.Now.ToString("yyyyMMdd").Substring(2, 6);
            var allRecord = Data.Dao.Get().QueryDictionaries("select * from sys_department");
            var srb = allRecord.Count(c => c["Code"].ToString().Contains(date)) + 1;
            return Convert.ToInt32(date + srb.ToString().PadLeft(2, '0'));
        }

    }
}
