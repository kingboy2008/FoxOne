using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace FoxOne.Business
{
    public enum YesOrNo
    {
        [Description("是")]
        True=1,

        [Description("否")]
        False=0
    }

    public enum DefaultStatus
    {
        [Description("启用")]
        Enabled,

        [Description("禁用")]
        Disabled
    }
}
