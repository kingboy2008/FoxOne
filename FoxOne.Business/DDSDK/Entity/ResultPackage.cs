using FoxOne.Data.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FoxOne.Business.DDSDK.Entity
{
    public class ResultPackage
    {
        /// <summary>  
        /// 错误码  
        /// </summary>  
        [Column(IsDataField = false)]
        public int ErrCode { get; set; } = -1;

        /// <summary>  
        /// 错误消息  
        /// </summary>  
        [Column(IsDataField = false)]
        public string ErrMsg { get; set; }

        /// <summary>  
        /// 结果的json形式  
        /// </summary>  
        [Column(IsDataField = false)]
        public String Json { get; set; }


        public bool IsOK()
        {
            return ErrCode == 0;
        }

        public override string ToString()
        {
            String info = $"{nameof(ErrCode)}:{ErrCode},{nameof(ErrMsg)}:{ErrMsg}";

            return info;
        }
    }

    public class TokenResult : ResultPackage
    {
        public string Access_token { get; set; }
    }
}
