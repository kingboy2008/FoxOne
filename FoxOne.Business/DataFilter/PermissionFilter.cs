using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FoxOne.Core;

namespace FoxOne.Business.DataFilter
{
    [DisplayName("数据权限过滤器")]
    public class PermissionFilter : DataFilterBase
    {
        [DisplayName("权限编码")]
        public string PermissionCode { get; set; }


        [DisplayName("子过滤器")]
        public IList<StaticDataFilter> DataFilters
        {
            get;
            set;
        }


        public override bool Filter(IDictionary<string, object> data)
        {
            var result = false;
            if (!DataFilters.IsNullOrEmpty())
            {
                string rule = Security.Sec.Provider.GetPermissionRule(PermissionCode);
                if (rule.IsNotNullOrEmpty())
                {
                    var filter = DataFilters.FirstOrDefault(o => o.PermissionCode.Equals(rule));
                    if (filter != null)
                    {
                        result = filter.Filter(data);
                    }
                }
            }
            return result;
        }
    }
}
