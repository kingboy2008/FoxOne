/*********************************************************
 * 作　　者：刘海峰
 * 联系邮箱：mailTo:liuhf@foxone.net
 * 创建时间：2014/6/8 17:48:43
 * 描述说明：
 * *******************************************************/
using System;
using System.ComponentModel;
using FoxOne.Core;
namespace FoxOne.Business
{
    [DisplayName("日期等于")]
    public class DateTimeEqualsOperation : ColumnOperator
    {
        public override bool Operate(object obj1, object obj2)
        {
            if (obj1 == null || obj2 == null || obj1.ToString().IsNullOrEmpty() || obj2.ToString().IsNullOrEmpty())
            {
                return false;
            }
            DateTime o1, o2;
            if (obj1 is DateTime && obj2 is DateTime)
            {
                o1 = (DateTime)obj1;
                o2 = (DateTime)obj2;
                return Compare(o1, o2);
            }
            if (DateTime.TryParse(obj1.ToString(), out o1) && DateTime.TryParse(obj2.ToString(), out o2))
            {
                return Compare(o1, o2);
            }
            return false;
        }

        protected virtual bool Compare(DateTime o1, DateTime o2)
        {
            return o1 == o2;
        }
    }

    [DisplayName("日期大于")]
    public class DateTimeGreaterThenOperation : DateTimeEqualsOperation
    {
        protected override bool Compare(DateTime i1, DateTime i2)
        {
            return i1 > i2;
        }
    }

    [DisplayName("日期大于或等于")]
    public class DateTimeGreaterOrEqualOperation : DateTimeEqualsOperation
    {
        protected override bool Compare(DateTime o1, DateTime o2)
        {
            return o1 > o2 || o1 == o2;
        }
    }



    [DisplayName("日期小于")]
    public class DateTimeLessThenOperation : DateTimeEqualsOperation
    {
        protected override bool Compare(DateTime i1, DateTime i2)
        {
            return i1 < i2;
        }
    }

    [DisplayName("日期小于或等于")]
    public class DateTimeLessOrEqualOperation : DateTimeEqualsOperation
    {
        protected override bool Compare(DateTime o1, DateTime o2)
        {
            return o1 < o2 || o1 == o2;
        }
    }
}