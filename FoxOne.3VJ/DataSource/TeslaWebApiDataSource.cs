using FoxOne.Business;
using FoxOne.Data.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Transactions;
using FoxOne.Core;
using FoxOne.Data;
using FoxOne.Controls;
using FoxOne.Business.Environment;
using FoxOne.Business.Security;
using FoxOne._3VJ.DataSource;
using System.Collections;
using System.IO;

namespace FoxOne._3VJ
{
    [DisplayName("特斯拉WebApi数据源")]
    public class TeslaWebApiDataSource : ListDataSourceBase, IFormService, IFlowFormService,IKeyValueDataSource
    {
        private const int NORMAL_CODE = 1;

        public string GetUrl { get; set; }

        public string InsertUrl { get; set; }

        public string UpdateUrl { get; set; }

        public string DeleteUrl { get; set; }

        public string AgreelUrl { get; set; }

        public string RejectUrl { get; set; }

        public string BaseUrl { get; set; }

        public string KeyName { get; set; }

        public string ParentKeyName { get; set; }

        public string TitleField { get; set; }

        public string ValueField { get; set; }

        public IDictionary<string, object> FormData { get; set; }

        protected override IEnumerable<IDictionary<string, object>> GetListInner()
        {
            string keyName = string.Empty;
            string keyValue = string.Empty;
            if (ParentKeyName.IsNotNullOrEmpty())
            {
                if (Parameter.IsNullOrEmpty() || !Parameter.ContainsKey(ParentKeyName))
                {
                    throw new ArgumentNullException(ParentKeyName);
                }
                keyName = ParentKeyName;
                keyValue = Parameter[keyName].ToString();
            }
            else
            {
                if (Parameter.IsNullOrEmpty() || !Parameter.ContainsKey(KeyName))
                {
                    throw new ArgumentNullException(KeyName);
                }
                keyName = KeyName;
                keyValue = Parameter[keyName].ToString();
            }
            var res = new List<IDictionary<string, object>>();
            GetDataFromWeb<ArrayList>(GetUrl, "{\"" + keyName + "\":\"" + keyValue + "\"}", arrList => {
                foreach (var item in arrList)
                {
                    res.Add(new Dictionary<string, object>(item as IDictionary<string, object>, StringComparer.InvariantCultureIgnoreCase));
                }
                return arrList;
            });
            return res;
        }

        public int Delete(string key)
        {
            if (DeleteUrl.IsNotNullOrEmpty())
            {
                var keyValue = key;
                if (keyValue.IsNullOrEmpty())
                {
                    keyValue = System.Web.HttpContext.Current.Request[KeyName];
                }
                return GetDataFromWeb<bool>(DeleteUrl, "{\"" + KeyName + "\":\"" + keyValue + "\"}") ? 1 : 0;
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
            return GetDataFromWeb<Dictionary<string, object>>(GetUrl, "{\"" + KeyName + "\":\"" + keyValue + "\"}", res => new Dictionary<string, object>(res, StringComparer.InvariantCultureIgnoreCase));
        }

        public int Insert(IDictionary<string, object> data)
        {
            if (InsertUrl.IsNotNullOrEmpty())
            {
                return GetDataFromWeb<bool>(InsertUrl, data) ? 1 : 0;
            }
            return 1;
        }

        public int Update(string key, IDictionary<string, object> data)
        {
            if (UpdateUrl.IsNotNullOrEmpty())
            {
                return GetDataFromWeb<bool>(UpdateUrl, data) ? 1 : 0;
            }
            return 1;
        }

        private T GetDataFromWeb<T>(string outerUrl, object data, Func<T,T> successCallback=null)
        {
            string jsonString = data is String ? data.ToString() : JSONHelper.Serialize(data);
            var result = JSONHelper.Deserialize<TeslaReturnObject<T>>(HttpHelper.Post($"{BaseUrl}{outerUrl}",jsonString));
            if (result.success && result.result.status == NORMAL_CODE)
            {
                if (result.result.returnObject != null)
                {
                    if (successCallback != null)
                    {
                        return successCallback(result.result.returnObject);
                    }
                    return result.result.returnObject;
                }
                return default(T);
            }
            throw new Exception(result.error == null ? result.result.errorMsg : result.error);
        }

        public bool CanRunFlow()
        {
            return true;
        }

        public IDictionary<string, object> SetParameter()
        {
            throw new NotImplementedException();
        }

        public void OnFlowFinish(string instanceId, string dataLocator, bool agree, string denyOption)
        {
            if (agree)
            {
                var response = HttpHelper.Post($"{BaseUrl}{AgreelUrl}", $"{{ \"agreementId\": \"{dataLocator}\",\"approvalRemark\": \"{denyOption}\"   }}");
                Logger.Info($"Tesla 合同通过回调 {response}");
            }
            else
            {
                var response = HttpHelper.Post($"{BaseUrl}{RejectUrl}", $"{{ \"agreementId\": \"{dataLocator}\",\"approvalRemark\": \"{denyOption}\"   }}");
                Logger.Info($"Tesla 合同不通过回调 {response}");
            }
        }

        public IEnumerable<TreeNode> SelectItems()
        {
            var result = new List<TreeNode>();
            
            try
            {
                GetDataFromWeb<ArrayList>(GetUrl, "{}", datas => {
                    foreach (var item in datas)
                    {
                        var dic = item as IDictionary<string, object>;
                        result.Add(new TreeNode() { Text = dic[TitleField].ToString(), Value = dic[ValueField].ToString() });
                    }
                    return datas;
                });
            }
            catch (Exception ex)
            {
                Logger.Error($"TeslaWebApiDataSource SelectItems {BaseUrl}{GetUrl} ", ex);
            }

            return result;
        }

        public object Converter(string columnName, object columnValue, IDictionary<string, object> rowData)
        {
            try
            {
                return this.Get(columnValue.ToString())[TitleField];
            }
            catch (Exception ex)
            {
                Logger.Error($"TeslaWebApiDataSource Converter {BaseUrl}{GetUrl}?{KeyName}={columnValue} ", ex);
            }
            return columnValue;
        }
    }


    [DisplayName("特斯拉附件数据源")]
    public class TeslaAttachDataSource : TeslaWebApiDataSource
    {

        protected override IEnumerable<IDictionary<string, object>> GetListInner()
        {
            Parameter[KeyName] = Parameter["RelateId"];///TODO:防止硬编码；
            var datas= base.GetListInner();
            var result = new List<AttachmentEntity>();
            foreach (var item in datas)
            {
                FileInfo info = new FileInfo(Path.Combine(SysConfig.AppSettings["FileSystem"], item["filePath"].ToString().Trim('/')));
                result.Add(new AttachmentEntity()
                {
                    CreateTime = info.CreationTime,
                    CreatorId = "系统",
                    FileName = info.Name,
                    FilePath = item["filePath"].ToString().Trim('/'),
                    FileSize = info.Length.ConvertTo<int>(),
                    FileType = info.Extension,
                    Id= item["filePath"].ToString().Trim('/'),
                });
            }
            return result.ToDictionary();
        }
    }

    public class TeslaReturnObject<T>
    {
        public bool success { get; set; }

        public string error { get; set; }

        public TeslaReturnInnerObject<T> result { get; set; }

        public bool unAuthorizedRequest { get; set; }
    }

    public class TeslaReturnInnerObject<T>
    {
        public string errorMsg { get; set; }

        public int status { get; set; }

        public T returnObject { get; set; }
    }
}
