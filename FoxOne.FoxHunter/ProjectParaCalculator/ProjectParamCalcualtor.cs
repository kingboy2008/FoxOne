using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using FoxOne.Core;

namespace FoxOne.FoxHunter
{
    [DisplayName("固定比例运算器")]
    public class ProjectParamCalcualtor : CalculatorBase
    {
        protected override decimal CalculateCount()
        {
            if( Regex.IsMatch(Context.Parameter.CountQuery, "^\\d+$"))
            {
                return decimal.Parse(Context.Parameter.CountQuery);
            }
            Dictionary<string, object> para = new Dictionary<string, object>();
            if (!Context.Parameter.CountParam.IsNullOrEmpty()) {
                foreach (var field in Context.Parameter.CountParam.Split(','))
                {
                    para[field]= Context.Project.ContainsKey(field) ? Context.Project[field] : string.Empty ;
                }
            }
            if (CountReferenceRate())
            {
                para[RATE_PARAM_NAME.Trim('#')] = Rate;
            }
            return Data.Dao.Get().QueryScalar<decimal>(Context.Parameter.CountQuery, para.IsNullOrEmpty()?null:para);
        }

        protected override decimal CalculateRate()
        {
            if (Regex.IsMatch(Context.Parameter.RateQuery, "^\\d+$"))
            {
                return decimal.Parse(Context.Parameter.RateQuery);
            }
            Dictionary<string, object> para = new Dictionary<string, object>();
            if (!Context.Parameter.RateParam.IsNullOrEmpty())
            {
                foreach (var field in Context.Parameter.RateParam.Split(','))
                {
                    para[field] = Context.Project.ContainsKey(field) ?  Context.Project[field]: string.Empty;
                }
            }
            if (RateReferenceCount())
            {
                para[COUNT_PARAM_NAME.Trim('#')] = Money;
            }
            return Data.Dao.Get().QueryScalar<decimal>(Context.Parameter.RateQuery, para.IsNullOrEmpty() ? null : para);
        }
    }
}
