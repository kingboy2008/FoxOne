using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using FoxOne.Core;

namespace FoxOne.FoxHunter
{
    public abstract class CalculatorBase
    {
        protected const string RATE_PARAM_NAME = "#RATE#";
        protected const string COUNT_PARAM_NAME = "#COUNT#";
        
        public decimal Rate { get; protected set; }

        public decimal Money { get; protected set; }

        public CalculatorContext Context { get; set; }

        protected abstract decimal CalculateRate();

        protected abstract decimal CalculateCount();

        public virtual bool Calculate()
        {
            if (RateReferenceCount() && CountReferenceRate())
            {
                return false;
            }
            if (!RateReferenceCount())
            {
                this.Rate = CalculateRate();
                this.Money = CalculateCount();
            }
            else
            {
                this.Money = CalculateCount();
                this.Rate = CalculateRate();
            }
            return true;
        }

        public virtual bool IsOverLimit()
        {
            var limit = this.Context.Parameter.Limit;
            if (limit.IsNullOrEmpty())
            {
                return true;
            }
            if (Regex.IsMatch(limit,"^\\d+(\\.\\d+)?%$"))
            {
                return limit.Trim('%').ConvertTo<decimal>().CompareTo(Rate)>0;
            }
            else
            {
                return limit.ConvertTo<decimal>().CompareTo(Money) > 0;
            }
        }

        protected bool RateReferenceCount()
        {
            return Context.Parameter.RateQuery.IsNotNullOrEmpty()&& Context.Parameter.RateQuery.Contains(COUNT_PARAM_NAME);
        }

        protected bool CountReferenceRate()
        {
            return Context.Parameter.CountQuery.IsNotNullOrEmpty()&& Context.Parameter.CountQuery.Contains(RATE_PARAM_NAME);
        }
    }

    public class CalculatorContext
    {
        public IDictionary<string,object> Project { get; set; }

        public ProjectParameterEntity Parameter { get; set; }
    }
}
