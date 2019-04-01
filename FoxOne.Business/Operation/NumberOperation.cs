/*********************************************************
 * 作　　者：刘海峰
 * 联系邮箱：mailTo:liuhf@foxone.net
 * 创建时间：2014/6/8 17:48:43
 * 描述说明：
 * *******************************************************/
using System.ComponentModel;
using FoxOne.Core;
namespace FoxOne.Business
{
    [DisplayName("数字等于")]
    public class NumberEqualsOperation : ColumnOperator
    {
        public override bool Operate(object obj1, object obj2)
        {
            if (obj1 == null || obj2 == null || obj1.ToString().IsNullOrEmpty() || obj2.ToString().IsNullOrEmpty())
            {
                return false;
            }
            double o1, o2;
            if (obj1 is double && obj2 is double)
            {
                o1 = (double)obj1;
                o2 = (double)obj2;
                return Compare(o1, o2);
            }
            if (double.TryParse(obj1.ToString(), out o1) && double.TryParse(obj2.ToString(), out o2))
            {
                return Compare(o1, o2);
            }
            return false;
        }

        protected virtual bool Compare(double o1, double o2)
        {
            return o1 == o2;
        }
    }

    [DisplayName("数字大于")]
    public class GreaterThenOperation : NumberEqualsOperation
    {
        protected override bool Compare(double i1, double i2)
        {
            return i1 > i2;
        }
    }

    [DisplayName("数字大于或等于")]
    public class GreaterOrEqualOperation : NumberEqualsOperation
    {
        protected override bool Compare(double o1, double o2)
        {
            return o1 > o2 || o1 == o2;
        }
    }



    [DisplayName("数字小于")]
    public class LessThenOperation : NumberEqualsOperation
    {
        protected override bool Compare(double i1, double i2)
        {
            return i1 < i2;
        }
    }

    [DisplayName("数字小于或等于")]
    public class LessOrEqualOperation : NumberEqualsOperation
    {
        protected override bool Compare(double o1, double o2)
        {
            return o1 < o2 || o1 == o2;
        }
    }
}