using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FoxOne.Business.DDSDK.Entity
{
    /// <summary>  
    /// 签名包  
    /// </summary>  
    public class SignPackage
    {
        public int agentId { get; set; }

        public String corpId { get; set; }

        public String timeStamp { get; set; }

        public String nonceStr { get; set; }

        public String signature { get; set; }

        public String url { get; set; }

        public string jsticket { get; set; }
    }
}
