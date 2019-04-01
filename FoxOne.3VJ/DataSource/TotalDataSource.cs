using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FoxOne.Business;
using FoxOne.Core;

namespace FoxOne._3VJ
{
    [DisplayName("累计数据源")]
    public class TotalDataSource:CRUDDataSource
    {
        /// <summary>
        /// 数量字段
        /// </summary>
        [DisplayName("数量字段")]
        public string Count { get; set; }

        /// <summary>
        /// 标题字段
        /// </summary>
        [DisplayName("标题字段")]
        public string Title { get; set; }

        public override IEnumerable<IDictionary<string, object>> GetList(int pageIndex, int pageSize, out int recordCount)
        {
            var list = GetList();
            if (list.IsNullOrEmpty())
            {
                recordCount = 0;
                return null;
            }
            recordCount = list.Count();
            var result = list.Skip((pageIndex - 1) * pageSize).Take(pageSize).ToList();//base.GetList(pageIndex, pageSize, out recordCount).ToList();
            var sum= list.Sum(c => c[Count].ConvertTo<int>());
            var totaltRow = new Dictionary<string, object>();
            totaltRow[Count] = sum;
            totaltRow[Title] = "总计";
            result.Add(totaltRow);
            return result;
        }
    }
}
