using FoxOne.Business;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FoxOne.Data;
using FoxOne.Core;
using FoxOne.Business.OAuth;
using System.Web;
using System.ComponentModel;

namespace FoxOne._3VJ
{
    /// <summary>
    /// 变更管理数据源
    /// </summary>
    [DisplayName("变更管理数据源")]
    public class ChangeManagementDataSource : CRUDDataSource
    {
        public override int Insert(IDictionary<string, object> data)
        {
            string codeTemplate = "3VJIT-{0}-{1}";
            string date = data["CreateTime"].ConvertTo<DateTime>().ToString("yyyy-MM-dd");
            var allRecord = Dao.Get().Select<WFormChangeManagementEntity>();
            int number = allRecord.Count(o => o.CreateTime.Date == DateTime.Now.Date) + 1;
            while (allRecord.Any(o => o.Code.IsNotNullOrEmpty() && o.Code.Equals(codeTemplate.FormatTo(date, number), StringComparison.OrdinalIgnoreCase)))
            {
                number++;
            }
            data["Code"] = codeTemplate.FormatTo(date, number);
            return base.Insert(data);
        }
    }

    public class CustomerResult
    {
        public bool success { get; set; }

        public CustomerResultInner result { get; set; }
    }
    public class CustomerResultInner
    {
        public int totalCount { get; set; }

        public List<Dictionary<string, object>> items { get; set; }
    }
}
