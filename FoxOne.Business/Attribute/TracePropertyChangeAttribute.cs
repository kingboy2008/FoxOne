using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FoxOne.Business
{
    /// <summary>
    /// 该特征指示所标识的属性是否需要记录历史变更
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class TracePropertyChangeAttribute:Attribute
    {
    }
}
