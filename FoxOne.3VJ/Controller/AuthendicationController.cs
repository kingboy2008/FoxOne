using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Http;
using FoxOne.Core;
using FoxOne.Data;
using FoxOne.Business.Security;

namespace FoxOne._3VJ.Controller
{
    /// <summary>
    /// TOKEN验证接口
    /// </summary>
    public class AuthendicationController : ApiController
    {
        /// <summary>
        /// 验证token是否从OA颁发
        /// </summary>
        /// <param name="appid">OA系统颁发给业务系统的唯一ID</param>
        /// <param name="token">本次需验证的TOKEN</param>
        /// <returns></returns>
        [HttpGet]
        public bool ValidateToken(int appid, string token)
        {
            if (appid == 0)
            {
                throw new FoxOneException("Parameter_Not_Null", "appid");
            }
            if (token.IsNullOrEmpty())
            {
                throw new FoxOneException("Parameter_Not_Null", "token");
            }
            var SSOSystem = Dao.Get().Query<SSOSystemToken>().FirstOrDefault(o => o.AppId == appid && o.Token == token);
            bool result = false;
            if (SSOSystem != null)
            {
                result = true;
                SSOSystem.IsUse = true;
                Dao.Get().Update<SSOSystemToken>(SSOSystem);
            }
            Logger.Info("System:单点登录，验证用户token，appid：{0}，token：{1}，result：{2}".FormatTo(appid, token, result));
            return result;
        }


    }
}
