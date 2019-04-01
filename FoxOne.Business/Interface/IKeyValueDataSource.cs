using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Mvc;

namespace FoxOne.Business
{
    public interface IKeyValueDataSource : IFieldConverter
    {
        IDictionary<string,object> FormData { get; set; }

        IEnumerable<TreeNode> SelectItems();
    }
}
