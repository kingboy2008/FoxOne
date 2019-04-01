using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FoxOne.Business.DDSDK.Entity;
using FoxOne.Core;


namespace FoxOne.Business.DDSDK.Service
{
    public static class DDUserService
    {
        private const string service = "user";

        public static string Create(DDUserCreateInfo info)
        {
            return RequestInner<DDUserCreateInfo>("create", true, info).userid;
        }

        public static bool Update(DDUserCreateInfo info)
        {
            return RequestInner<DDUserCreateInfo>("update", true, info).IsOK();
        }

        public static bool Delete(string id)
        {
            return RequestInner<DDUserCreateInfo>("delete", false, id).IsOK();
        }

        public static IList<DDUserInfo> Get(int deptId)
        {
            var result = RequestInner<DDUserPackage>("list", false, deptId);
            return result.userlist;
        }

        public static DDUserInfo Get(string id)
        {
            var result = RequestInner<DDUserInfo>("get", false, id);
            return result;
        }


        public static T RequestInner<T>(string method, bool isPost, object parameter)
        {
            string idParam = string.Empty;
            if (parameter is string)
            {
                idParam = "&userid=" + parameter;
            }
            if(parameter is int)
            {
                idParam = "&department_id=" + parameter;
            }
            string apiurl = $"{Urls.DDApiUrl}{service}/{method}?{Keys.access_token}={DDHelper.GetAccessToken()}{idParam}";
            var result = JSONHelper.Deserialize<T>(isPost ? HttpHelper.Post(apiurl, JSONHelper.Serialize(parameter)) : HttpHelper.Get(apiurl));
            if ((result as ResultPackage).IsOK())
            {
                return result;
            }
            throw new FoxOneException((result as ResultPackage).ErrMsg);
        }
    }
}
