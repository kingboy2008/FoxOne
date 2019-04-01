using FoxOne.Business;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FoxOne.Core;
using FoxOne.Business.Security;
using FoxOne.Data;
using System.Web;

namespace FoxOne._3VJ.DataSource
{
    public class SSOSystemDataSource:CRUDDataSource
    {
        public override int Insert(IDictionary<string, object> data)
        {
            var ssosystem = data.ToEntity<SSOSystem>();
            ssosystem.AppId = ssosystem.AgentId;
            ssosystem.AppSecret = Utility.GetGuid().ToLower();
            ssosystem.CreateTime = DateTime.Now;
            ssosystem.CreatorId = Sec.User.Id;
            ssosystem.Id = Utility.GetGuid().ToLower();
            return base.Insert(ssosystem.ToDictionary());
        }
    }

    public class SSOService
    {
        public SSOSystem Get(int appid)
        {
            return Dao.Get().Query<SSOSystem>().FirstOrDefault(o => o.AppId == appid);
        }

        public string GenerateToken(int appid)
        {
            var ssoSystem = Get(appid);
            if(ssoSystem == null)
            {
                throw new FoxOneException("System_Not_Found", appid.ToString());
            }
            string source = $"{{Id:'{Sec.User.Id}',T:'{Utility.GetGuid().Substring(16)}'}}";
            var token = AESCrypToHelper.EncryptAes(source, ssoSystem.AppSecret);
            ssoSystem.Token = token;
            Dao.Get().Insert(new SSOSystemToken() { Id = Utility.GetGuid().ToLower(), AppId = appid, IsUse = false, RentId = 1, Token = token });
            return ssoSystem.LogOnUrl + "?token=" + HttpUtility.UrlEncode(token);
        }
    }
}
