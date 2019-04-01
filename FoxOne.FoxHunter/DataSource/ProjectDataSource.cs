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
    [DisplayName("项目数据源")]
    public class ProjectDataSource :CRUDDataSource
    {
        private static readonly string COMMON_KEY= "df859cde95a5424e991826743289c95d";

        //private CRUDDataSource _projectWorkItemCRUD;
        private CRUDEntity _projectWorkItemCRUD;

        private CRUDEntity ProjectWorkItemCRUD
        {
            get
            {
                if (_projectWorkItemCRUD == null)
                {
                    var curdEnt = DBContext<CRUDEntity>.Instance.Get("projworkitem");
                    //_projectWorkItemCRUD = new CRUDDataSource() { CRUDName = "projworkitem" };
                    _projectWorkItemCRUD = curdEnt;
                }
                return _projectWorkItemCRUD;
                //return curdEnt;
            }
        }

        private CRUDEntity _projectParameterCRUD;

        private CRUDEntity ProjectParameterCRUD
        {
            get
            {
                if (_projectParameterCRUD == null)
                {
                    var curdEnt = DBContext<CRUDEntity>.Instance.Get("projProjectParameter");
                    _projectParameterCRUD = curdEnt;
                }
                return _projectParameterCRUD;
            }
        }

        public override int Insert(IDictionary<string, object> data)
        {
            int projectCount= base.Insert(data);
            if (projectCount > 0)
            {
                projectCount += InsertProjectWorkItem(data[KeyFieldName].ToString());
                projectCount += InsertProjectParameter(data[KeyFieldName].ToString());
            }
            return projectCount;
        }

        private int InsertProjectWorkItem(string key)
        {
            var result = 0;
            var parameters = new Dictionary<string, object>() { { "ProjectId", COMMON_KEY } };
            //ProjectWorkItemCRUD.Parameter = parameters;
            Dictionary<string, string> keyMapping = new Dictionary<string, string>();
            var workitems = Data.Dao.Get().QueryDictionaries("SELECT * FROM proj_workitem WHERE ProjectId=#ProjectId#", parameters);//Data.Dao.Get().QueryDictionaries(ProjectWorkItemCRUD.SelectSQL,parameters);
            workitems.ForEach(c => {
                c["ProjectId"] = key;
                string newPk= Utility.GetGuid();
                keyMapping[c["Id"].ToString()] = newPk;
                c["Id"] = newPk;
                //Data.Dao.Get().ExecuteNonQuery(ProjectWorkItemCRUD.InsertSQL, c);

            });
            workitems.ForEach(c => {
                if (c["ParentId"] != null && c["ParentId"].ToString().IsNotNullOrEmpty())
                {
                    c["ParentId"] = keyMapping[c["ParentId"].ToString()];
                }
                result += Data.Dao.Get().ExecuteNonQuery(ProjectWorkItemCRUD.InsertSQL, c);
            });
            return result;
        }

        private int InsertProjectParameter(string key)
        {
            var result = 0;
            var parameters = new Dictionary<string, object>() { { "ProjectId", COMMON_KEY } };
            var workitems = Data.Dao.Get().QueryDictionaries("SELECT * FROM proj_project_parameter WHERE ProjectId=#ProjectId#", parameters);//Data.Dao.Get().QueryDictionaries(ProjectWorkItemCRUD.SelectSQL,parameters);
            workitems.ForEach(c => {
                c["ProjectId"] = key;
                c["Id"] = Utility.GetGuid();
                result= Data.Dao.Get().ExecuteNonQuery(ProjectParameterCRUD.InsertSQL, c);
            });
            return result;
        }

        public override int Delete(string key)
        {
            return 0;
            int result = base.Delete(key);
            if (result > 0)
            {
                result += DeleteProjectWorkItem(key);
                result += DeleteProjectParameter(key);
            }
            return result;
        }

        private int DeleteProjectWorkItem(string key)
        {
            var parameters = new Dictionary<string, object>() { { "ProjectId", key } };
            return Data.Dao.Get().ExecuteNonQuery(ProjectWorkItemCRUD.DeleteSQL, parameters);
        }

        private int DeleteProjectParameter(string key)
        {

            var parameters = new Dictionary<string, object>() { { "ProjectId", key } };
            return Data.Dao.Get().ExecuteNonQuery(ProjectParameterCRUD.DeleteSQL, parameters);
        }
    }
}
