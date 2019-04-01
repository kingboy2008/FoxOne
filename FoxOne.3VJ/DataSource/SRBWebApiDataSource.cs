using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FoxOne.Business;
using FoxOne.Core;

namespace FoxOne._3VJ
{
    [DisplayName("SRBWebApi数据源")]
    public class SRBWebApiDataSource : ListDataSourceBase, IFormService, IFlowFormService
    {
        private const int NORMAL_CODE = 200;

        public string GetUrl { get; set; }

        public string InsertUrl { get; set; }

        public string DeleteUrl { get; set; }

        public string UpdateUrl { get; set; }

        public string BaseUrl { get; set; }

        public string KeyName { get; set; }

        public string ParentKeyName { get; set; }

        public bool CanRunFlow()
        {
            return true;
        }

        protected override IEnumerable<IDictionary<string, object>> GetListInner()
        {
            string queryParameter = string.Empty;
            ///有设主键时才拼接QueryString
            if (ParentKeyName.IsNotNullOrEmpty())
            {
                if (Parameter.IsNullOrEmpty() || !Parameter.ContainsKey(ParentKeyName))
                {
                    throw new ArgumentNullException(ParentKeyName);
                }
                var key = Parameter[ParentKeyName];
                queryParameter = $"?{ParentKeyName}={key}";
            }
            var result = JSONHelper.Deserialize<SRBReturnObject<ArrayList>>(HttpHelper.Get($"{BaseUrl}{GetUrl}{queryParameter}"));
            ArrayList al = new ArrayList();
            if (result.success && result.code == NORMAL_CODE)
            {
                var res = new List<IDictionary<string, object>>();
                var datas = result.result as ArrayList;
                foreach (var item in datas)
                {
                    var newItem = new Dictionary<string, object>(item as IDictionary<string, object>, StringComparer.InvariantCultureIgnoreCase);
                    //var keys = newItem.Keys.Where(c => c.EndsWith("date")).ToList();
                    //foreach (var key in keys)
                    //{
                    //    if (newItem.ContainsKey($"{key}str"))
                    //        newItem[key] = newItem[$"{key}str"];
                    //}
                    res.Add(newItem);
                }
                return res;
            }
            throw new Exception(result.errorMessage.IsNullOrEmpty() ? result.code.ToString() : result.errorMessage);
        }

        public int Delete(string key)
        {
            var keyValue = key;
            if (keyValue.IsNullOrEmpty())
            {
                keyValue = System.Web.HttpContext.Current.Request[KeyName];
            }
            if (keyValue.IsNullOrEmpty())
            {
                throw new ArgumentNullException(KeyName);
            }
            var result = JSONHelper.Deserialize<SRBReturnObject<object>>(HttpHelper.Delete($"{BaseUrl}{DeleteUrl}?{KeyName}={keyValue}"));
            if (result.success && result.code == NORMAL_CODE)
            {
                return result.result.ConvertTo<int>() ;
            }
            return 1;
        }

        public IDictionary<string, object> Get(string key)
        {
            var keyValue = key;
            if (keyValue.IsNullOrEmpty())
            {
                keyValue = System.Web.HttpContext.Current.Request[KeyName];
            }
            if (keyValue.IsNullOrEmpty())
            {
                throw new ArgumentNullException(KeyName);
            }
            var result = JSONHelper.Deserialize<SRBReturnObject<Dictionary<string, object>>>(HttpHelper.Get($"{BaseUrl}{GetUrl}?{KeyName}={keyValue}"));
            if ((result.success && result.code == NORMAL_CODE))
            {
                var res= new Dictionary<string, object>(result.result, StringComparer.InvariantCultureIgnoreCase);
                //var keys = res.Keys.Where(c=>c.EndsWith("date")).ToList();
                //foreach (var item in keys)
                //{
                //    if (res.ContainsKey($"{item}str"))
                //        res[item] = res[$"{item}str"];
                //}
                return res;
            }
            throw new Exception(result.errorMessage.IsNullOrEmpty()?result.code.ToString():result.errorMessage);
        }

        public int Insert(IDictionary<string, object> data)
        {
            if (InsertUrl.IsNotNullOrEmpty())
            {
                data["approvestatus"] = 0;
                data["billdate"] = DateTime.Now.ToString("yyyy-MM-dd");
                if (data.ContainsKey("Id"))
                {
                    data[KeyName] = data["Id"];
                }
                if (!data.ContainsKey(KeyName) || data[KeyName] == null||data[KeyName].ToString().IsNullOrEmpty())
                {
                    data[KeyName] = Utility.GetGuid();
                }
                var result = JSONHelper.Deserialize<SRBReturnObject<object>>(HttpHelper.Post($"{BaseUrl}{InsertUrl}", JSONHelper.Serialize(data)));
                if (result.success && result.code == NORMAL_CODE)
                {
                    return result.result.ConvertTo<int>() ;
                }
                throw new Exception(result.errorMessage.IsNullOrEmpty() ? result.code.ToString() : result.errorMessage);
            }
            return 1;
        }

        public IDictionary<string, object> SetParameter()
        {
            throw new NotImplementedException();
        }

        public int Update(string key, IDictionary<string, object> data)
        {
            if (UpdateUrl.IsNotNullOrEmpty())
            {
                data["approvestatus"] = 1;
                var result = JSONHelper.Deserialize<SRBReturnObject<object>>(HttpHelper.Put($"{BaseUrl}{UpdateUrl}", JSONHelper.Serialize(data)));
                if (result.success && result.code == NORMAL_CODE)
                {
                    return result.result.ConvertTo<int>();
                }
                throw new Exception(result.errorMessage.IsNullOrEmpty() ? result.code.ToString() : result.errorMessage);
            }
            return 1;
        }

        public void OnFlowFinish(string instanceId, string dataLocator, bool agree, string denyOption)
        {
            if (UpdateUrl.IsNotNullOrEmpty())
            {
                Dictionary<string, object> data = new Dictionary<string, object>() { };
                data["approvestatus"] = agree ? 2 : 3;
                data[KeyName] = dataLocator;
                var result = JSONHelper.Deserialize<SRBReturnObject<object>>(HttpHelper.Put($"{BaseUrl}{UpdateUrl}", JSONHelper.Serialize(data)));
                if (result.success && result.code == NORMAL_CODE)
                {
                    //return result.result.returnObject.ConvertTo<bool>() ? 1 : 0;
                    return;
                }
                throw new Exception(result.errorMessage.IsNullOrEmpty() ? result.code.ToString() : result.errorMessage);
            }
        }
    }



    public class SRBReturnObject<T>
    {
        public bool success { get; set; }

        public string errorMessage { get; set; }

        public T result { get; set; }

        public int code { get; set; }
    }
}
