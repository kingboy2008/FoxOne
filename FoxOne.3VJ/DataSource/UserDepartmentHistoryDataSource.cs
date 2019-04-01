using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FoxOne.Business;
using FoxOne.Core;

namespace FoxOne._3VJ.DataSource
{
    [DisplayName("员工历史部门数据源")]
    public class UserDepartmentHistoryDataSource:ListDataSourceBase
    {
        protected override IEnumerable<IDictionary<string, object>> GetListInner()
        {
            var datas = DBContext<PropertyChangeRecord>.Instance.Where(c => c.TypeName.Equals("FoxOne.Business.User") && c.PropertyName.Equals("DepartmentId"));
            var result= datas.ToDictionary();
            //foreach (var row in result)
            //{
            //    //var user = DBContext<IUser>.Instance.FirstOrDefault(c => c.LoginId.Equals(row["PKValue"].ToString()));
            //    //if (user != null)
            //    //{
            //    //    row["PKValue"] =user.Name;
            //    //}
            //}
            return result;
        }
    }
}
