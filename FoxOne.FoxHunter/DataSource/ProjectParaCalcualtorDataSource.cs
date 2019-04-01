using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FoxOne.Business;
using FoxOne.Core;

namespace FoxOne.FoxHunter
{
    [Category("FoxHunter")]
    [DisplayName("项目参数运算器")]
    public class ProjectParaCalcualtorDataSource : KeyValueDataSourceBase
    {
        private IEnumerable<TreeNode> selectItems;

        public override IEnumerable<TreeNode> SelectItems()
        {
            if (selectItems.IsNullOrEmpty())
            {
                var types = TypeHelper.GetAllSubType<CalculatorBase>();
                var datas = new List<TreeNode>();
                foreach (Type item in types)
                {
                    var tn = new TreeNode() {
                         Text= item.GetDisplayName(),
                          Value=item.FullName
                    };
                    datas.Add(tn);
                }
                selectItems = datas;
            }
            return selectItems;
        }
    }
}
