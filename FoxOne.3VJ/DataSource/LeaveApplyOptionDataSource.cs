using FoxOne.Business;
using FoxOne.Business.Security;
using FoxOne.Core;
using FoxOne.Data;
using FoxOne.Data.Attributes;
using FoxOne.Workflow.Business;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Transactions;
namespace FoxOne._3VJ.DataSource
{
    /// <summary>
    /// 可选调休数据源
    /// </summary>
    [DisplayName("可选调休数据源")]
    public class LeaveApplyOptionDataSource : KeyValueDataSourceBase
    {
        public override IEnumerable<TreeNode> SelectItems()
        {
            var userId = (FormData.IsNullOrEmpty() || !FormData.Keys.Contains("CreatorId")) ? Sec.User.Id : FormData["CreatorId"].ToString();
            //把我的已通过审批的加班申请找出来
            var items = Dao.Get().QueryEntities<OTApplyEntity>("SELECT l.* FROM wf_form_otapply l inner join wfl_instance i on l.Id=i.DataLocator where l.OTType=1 and l.CreatorId=#CreatorId# and i.FlowTag=2 ORDER BY OTBeginTime DESC", new { CreatorId = userId });
            if (items.IsNullOrEmpty())
            {
                //没有可用的加班可以调休
                return new List<TreeNode>() { new TreeNode() { Value = "", Text = "没有可用来调休的加班记录" } };
            }
            return items.Select(item => new TreeNode() { Value = item.Id, Text = $"{item.OTBeginTime}（{SysConfig.DayOfWeekCN[(int)item.OTBeginTime.DayOfWeek]}）至{item.OTEndTime}（{SysConfig.DayOfWeekCN[(int)item.OTEndTime.DayOfWeek]}）共{item.OTTimeLast}小时" });
        }
    }

    /// <summary>
    /// 加班/调休数据源
    /// </summary>
    [DisplayName("加班/调休数据源")]
    public class OTApplyDataSource : CRUDDataSource
    {
        public override int Insert(IDictionary<string, object> data)
        {
            var entity = data.ToEntity<OTApplyEntity>();
            if (entity.OTEndTime <= entity.OTBeginTime)
            {
                throw new Exception("结束时间必须大于开始时间");
            }
            entity.OTTimeLast = (decimal)(entity.OTEndTime - entity.OTBeginTime).TotalHours;
            if (entity.OTType == 2)
            {
                //2是调休申请
                //把已经通过审批或正在审批中的调休申请拿出来
                var tx = Dao.Get().QueryScalarList<string>("SELECT l.RelateId FROM wf_form_otapply l inner join wfl_instance i on l.Id=i.DataLocator where l.OTType=2 and i.CreatorId=#CreatorId# and i.FlowTag<=2", data);
                if (!tx.IsNullOrEmpty())
                {
                    if (tx.Contains(entity.RelateId))
                    {
                        throw new Exception("该加班登记记录已申请调休，不能重复申请调休！");
                    }
                }

                /*
                var ot = Dao.Get().Query<OTApplyEntity>().FirstOrDefault(o=>o.Id==entity.RelateId);
                if (ot.OTTimeLast != entity.OTTimeLast)
                {
                    throw new Exception("调休小时数只能等于加班小时数！");
                }*/
            }
            else if (entity.OTType == 1)
            {
                //1是加班申请
            }
            else if (entity.OTType >= 30 && entity.OTType < 500)//仅当天
            {
                entity.OTEndTime = entity.OTBeginTime.AddMinutes(1);
                entity.OTTimeLast = 0;
            }
            return base.Insert(entity.ToDictionary());
        }

        public override int Update(string key, IDictionary<string, object> data)
        {
            data["Id"] = key;
            var entity = data.ToEntity<OTApplyEntity>();
            if (entity.OTEndTime <= entity.OTBeginTime)
            {
                throw new Exception("结束时间必须大于开始时间");
            }
            entity.OTTimeLast = (decimal)(entity.OTEndTime - entity.OTBeginTime).TotalHours;
            if (entity.OTType == 2)
            {
                //2是调休申请
                //把已经通过审批或正在审批中的调休申请拿出来
                var tx = Dao.Get().QueryScalarList<string>("SELECT l.RelateId FROM wf_form_otapply l inner join wfl_instance i on l.Id=i.DataLocator where l.OTType=2 and i.CreatorId=#CreatorId# and i.FlowTag<=2 and l.Id<>#Id#", data);
                if (!tx.IsNullOrEmpty())
                {
                    if (tx.Contains(entity.RelateId))
                    {
                        throw new Exception("该加班登记记录已申请调休，不能重复申请调休！");
                    }
                }
            }
            return base.Update(key, entity.ToDictionary());
        }
    }
}
