using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FoxOne.Business.DDSDK
{
    /// <summary>  
    /// Url的Key  
    /// </summary>  
    public sealed class Keys
    {
        public const string corpid = "corpid";

        public const string corpsecret = "corpsecret";

        public const string department_id = "department_id";

        public const string userid = "userid";

        public const string chatid = "chatid";

        public const string access_token = "access_token";

        public const string jsapi_ticket = "jsapi_ticket";

        public const string noncestr = "noncestr";

        public const string timestamp = "timestamp";

        public const string url = "url";

        public const string code = "code";

        /// <summary>  
        /// 缓存的JS票据的KEY  
        /// </summary>  
        public const string CACHE_JS_TICKET_KEY = "CACHE_JS_TICKET_KEY";

        public const string CACHE_ACCESS_TOKEN = "CACHE_DD_ACCESS_TOKEN";

        /// <summary>  
        /// 缓存时间  
        /// </summary>  
        public const int CACHE_TIME = 7000;
    }
}
