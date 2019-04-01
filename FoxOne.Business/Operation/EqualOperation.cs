/*********************************************************
 * 作　　者：刘海峰
 * 联系邮箱：mailTo:liuhf@foxone.net
 * 创建时间：2014/6/8 17:48:43
 * 描述说明：
 * *******************************************************/
using System;
using System.ComponentModel;

namespace FoxOne.Business
{
    [DisplayName("字符等于")]
    public class EqualsOperation : ColumnOperator
    {
        public override bool Operate(object obj1, object obj2)
        {
            if (obj1 == obj2) return true;
            if (obj1 == null)
            {
                obj1 = string.Empty;
            }
            if (obj2 == null)
            {
                obj2 = string.Empty;
            }
            return string.Equals(obj1.ToString().Trim(), obj2.ToString().Trim(), StringComparison.CurrentCultureIgnoreCase);
        }
    }




}