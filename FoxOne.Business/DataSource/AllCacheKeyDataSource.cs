using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using FoxOne.Core;
using System.ComponentModel;
namespace FoxOne.Business
{
    //[Category("None")]
    [DisplayName("所有缓存键值")]
    public class AllCacheKeyDataSource : ListDataSourceBase,IFormService
    {
        public int Delete(string key)
        {
            //throw new NotImplementedException();
            return CacheHelper.Remove(key)?1:0;
        }

        public IDictionary<string, object> Get(string key)
        {
            throw new NotImplementedException();
        }

        public int Insert(IDictionary<string, object> data)
        {
            throw new NotImplementedException();
        }

        public int Update(string key, IDictionary<string, object> data)
        {
            throw new NotImplementedException();
        }

        protected override IEnumerable<IDictionary<string, object>> GetListInner()
        {
            var returnValue = new List<IDictionary<string, object>>();
            string queryString = HttpContext.Current.Request["SearchKey"];
            foreach (var key in CacheHelper.AllKeys)
            {
                if (queryString.IsNullOrEmpty() || key.IndexOf(queryString,StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    var dict = new Dictionary<string, object>();
                    dict["Key"] = key;
                    dict["Value"] = CacheHelper.GetValue(key).ToString();
                    dict["Id"]=dict["Key"];
                    returnValue.Add(dict);
                }
            }
            return returnValue;
        }
    }
}
