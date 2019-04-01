using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FoxOne.Controls
{
    public interface IChildFormControl
    {
        IList<FormControlBase> ChildrenFields { get; set; }
    }
}
