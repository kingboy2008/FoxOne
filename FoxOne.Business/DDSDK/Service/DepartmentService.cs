using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FoxOne.Business.DDSDK.Entity;
using FoxOne.Core;

namespace FoxOne.Business.DDSDK.Service
{
    public static class DDDepartmentService
    {
        private const string service = "department";

        public static int Create(DDDepartmentInfo info)
        {
            return RequestInner<DDDepartmentInfo>("create", true, info).id;
        }

        public static int Update(DDDepartmentInfo info)
        {
            return RequestInner<DDDepartmentInfo>("update", true, info).id;
        }

        public static int Delete(int id)
        {
            return RequestInner<DDDepartmentInfo>("delete", false, id).id;
        }

        public static IList<DDDepartmentInfo> Get()
        {
            var result = RequestInner<DDDepartmentPackage>("list", false, 1);
            return result.department;
        }

        public static DDDepartmentInfo Get(int id)
        {
            var result = RequestInner<DDDepartmentInfo>("get", false, id);
            return result;
        }


        public static T RequestInner<T>(string method, bool isPost, object parameter)
        {
            ;
            string idParam = string.Empty;
            if (parameter is int)
            {
                idParam = "&id=" + parameter;
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
