using FoxOne.Data.Mapping;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FoxOne.Controls
{
    public interface IFieldDefaultSetting
    {
        FormControlBase GetFormControl(Column field);

        TableColumn GetTableColumn(Column field);
    }
}
