using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FoxOne.FoxHunter
{
    public enum WorktimeStatus
    {
        [Description("已提交")]
        Submitted,
        [Description("驳回")]
        Reject,
        [Description("通过")]
        Pass,
    }
}
