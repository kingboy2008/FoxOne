using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using FoxOne.Business;
using FoxOne.Business.Security;
using FoxOne.Core;

namespace FoxOne.FoxHunter
{
    [Category("FoxHunter")]
    [DisplayName("工时数据源")]
    public class WorktimeDatasource:CRUDDataSource
    {

        public static readonly string PM_ROLE_ID = "3b4fe7d10828406c800753fbf2c135d6";

        public override IDictionary<string, object> Get(string key)
        {
            var data= base.Get(key);

            var projectId = data["ProjectId"].ToString();
            var param = new Dictionary<string, object>();
            param["UserId"] = Sec.User.Id;
            param["ProjectId"] = projectId;
            param["RoleId"] = PM_ROLE_ID;
            data["btnDeleteShow"] = "0";
            if(data["Status"].ConvertTo<WorktimeStatus>()!= WorktimeStatus.Pass&& Data.Dao.Get().QueryScalar<int>("SELECT COUNT(*) FROM proj_project_user_rel WHERE ProjectId=#ProjectId# AND UserId=#UserId# AND RoleId=#RoleId#", param) > 0)
            {
                data["btnRejectShow"] = "1";
                data["btnPassShow"] = "1";
            }
            else
            {
                data["btnRejectShow"] = "0";
                data["btnPassShow"] = "0";
            }
            data["btnSubmitShow"] = (data["UserId"].ToString().Equals(Sec.User.Id,StringComparison.CurrentCultureIgnoreCase)&& data["Status"].ConvertTo<WorktimeStatus>() != WorktimeStatus.Pass)?"1":"0";
            data["btnDeleteShow"] = (data["btnPassShow"].ToString().Equals("1") || data["btnSubmitShow"].ToString().Equals("1"))?"1":"0";
            return data;
        }

        public override int Update(string key, IDictionary<string, object> data)
        {
            var checkAction =data.ContainsKey("CheckAction")? data["CheckAction"].ToString():"";
            switch (checkAction)
            {
                case "Reject":
                    Reject(data);
                    break;
                case "Pass":
                    Pass(data);
                    break;
                default:
                    data["Status"] = WorktimeStatus.Submitted;
                    break;
            }
            return base.Update(key, data);
        }

        private void Reject(IDictionary<string, object> data)
        {
            data["Status"] = WorktimeStatus.Reject;
            data["CheckerId"] = Sec.User.Id;
            data["CheckTime"] = DateTime.Now;
        }

        private void Pass(IDictionary<string,object> data)
        {
            data["Status"] = WorktimeStatus.Pass;
            data["CheckerId"] = Sec.User.Id;
            data["CheckTime"] = DateTime.Now;
        }
    }
}
