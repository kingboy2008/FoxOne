using FoxOne.Business;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FoxOne.Core;
using FoxOne.Business.Security;
using FoxOne.Data;
using System.Web;
using FoxOne.Business.DDSDK;
using System.ComponentModel;

namespace FoxOne._3VJ
{
    /// <summary>
    /// 通知公告
    /// </summary>
    [DisplayName("通知公告")]
    public class NoticeDataSource : CRUDDataSource
    {
        public override int Insert(IDictionary<string, object> data)
        {
            data[KeyFieldName] = Utility.GetGuid();
            string targetType = data["TargetType"].ToString();
            if (targetType != "4")
            {
                bool toAllUser = targetType == "1";
                string title = data["Title"].ToString();
                string content = data["Content"].ToString();
                if (toAllUser)
                {
                    DDHelper.SendDDLinkMessage("http://oa.3weijia.com/dd/articledetail/" + data[KeyFieldName], title, content, true);
                }
                else
                {
                    string userList = targetType == "2" ? data["TargetIds"].ToString() : string.Empty;
                    string deptList = targetType == "3" ? data["TargetIds"].ToString() : string.Empty;
                    DDHelper.SendDDLinkMessage("http://oa.3weijia.com/dd/articledetail/" + data[KeyFieldName], title, content, false, userList, deptList);
                }
            }
            return base.Insert(data);
        }
    }
}
