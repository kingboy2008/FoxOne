using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FoxOne.Business;
using FoxOne.Core;
using FoxOne.Data.Attributes;

namespace FoxOne._3VJ
{
    /// <summary>
    /// 报销明细
    /// </summary>
    [DisplayName("报销明细")]
    public class SrbBxDetailFormService : CRUDDataSource
    {
        public override int Insert(IDictionary<string, object> data)
        {
            //data["ts"] = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            var result = base.Insert(data);
            UpdateMain(data["bxid"].ToString());
            return result;
        }

        public override int Update(string key, IDictionary<string, object> data)
        {
            //data["ts"] = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            var result = base.Update(key, data);
            UpdateMain(data["bxid"].ToString());
            return result;
        }

        public override int Delete(string key)
        {
            var data = base.Get(key);
            var result = base.Delete(key);
            UpdateMain(data["bxid"].ToString());
            return result;
        }

        private void UpdateMain(string key)
        {
            var datas = Data.Dao.Get().Query<BxDetailEntity>().Where(c => c.bxid == key).ToList();
            var main = Data.Dao.Get().QueryDictionaries("SELECT * FROM srb_bx WHERE bxid=#Id#", new { Id = key }).FirstOrDefault();
            ComputeCount(datas, main);
            ComputeWorkflowRoute(datas, main);
            Data.Dao.Get().ExecuteNonQuery(" UPDATE srb_bx SET totalamount=#totalamount#,repaytocompay=#repaytocompay#,paymenttoempo=#paymenttoempo#,workflowroute=#workflowroute# where bxid=#bxid# ", main);
        }

        private void ComputeCount(IEnumerable<BxDetailEntity> datas, IDictionary<string, object> main)
        {
            var sum = 0m;
            if (!datas.IsNullOrEmpty())
            {
                sum = datas.Sum(c => c.amount);
            }
            var clear = 0m;
            if (main.ContainsKey("cleanborrow") && main["cleanborrow"] != null)
            {
                decimal.TryParse(main["cleanborrow"].ToString(), out clear);
            }
            var delta = clear - sum;
            main["totalamount"] = sum;
            if (delta > 0)
            {
                main["repaytocompay"] = delta;
                main["paymenttoempo"] = 0m;
            }
            else
            {
                main["repaytocompay"] = 0m;
                main["paymenttoempo"] = -delta;
            }
        }

        private void ComputeWorkflowRoute(IEnumerable<BxDetailEntity> datas, IDictionary<string, object> main)
        {
            if (datas.IsNullOrEmpty())
            {
                main["workflowroute"] = "";
                return;
            }
            Dictionary<string, decimal> typeSum = new Dictionary<string, decimal>();
            FastType t = FastType.Get(typeof(BxDetailEntity));
            foreach (var ti in typeMapping)
            {
                typeSum[ti.Key] = 0;
                datas.ForEach(c => {
                    ti.Value.ForEach(i => {
                        try
                        {
                            typeSum[ti.Key] += t.GetGetter(i).GetValue(c).ConvertTo<decimal>();
                        }
                        catch (Exception ex)
                        {
                            Logger.Error(i, ex);
                        }

                    });
                });
            }
            Dictionary<string, LevelConfig> levelList = new Dictionary<string, LevelConfig>();
            foreach (var tsi in typeSum)
            {
                var lv = configMapping[tsi.Key].FirstOrDefault(c => (tsi.Value >= c.Min && tsi.Value > 0) && tsi.Value < c.Max);
                if (lv != null)
                {
                    levelList[tsi.Key] = lv;
                }
            }
            if (!levelList.IsNullOrEmpty())
            {
                main["workflowroute"] = levelList.Values.OrderByDescending(c => c.Level).ThenBy(c => c.Route).FirstOrDefault().Route;
            }
            else
            {
                main["workflowroute"] = "1";
            }
        }

        //类别定义
        Dictionary<string, List<string>> typeMapping = new Dictionary<string, List<string>>() {
            {"团队活动",new List<string>(){"activity"} },
            {"业务招待",new List<string>(){"entertainment"} },
            {"日常办公",new List<string>(){"officesupplies","maintenance","rollcharge","gasoline"} },
            {"固定资产",new List<string>(){"fixassets"} },
            {"电话通讯费",new List<string>(){"communication"} },
            {"水电租赁",new List<string>(){"waterele"} },
            {"广告",new List<string>(){"ads"} },
            {"退客户",new List<string>(){"giveback"} },
            {"员工差旅",new List<string>(){ "travelexpense", "quarterage", "tablemoney", "transportmoney" } },
            {"其他",new List<string>(){"other"} },



        };

        Dictionary<string, List<LevelConfig>> configMapping = new Dictionary<string, List<LevelConfig>>() {
            {"团队活动",new List<LevelConfig>(){
                new LevelConfig(){ Min=0,Max=2000, Level=1, Route="1" },
                new LevelConfig(){ Min=2000,Max=30000, Level=3, Route="3" },
                new LevelConfig(){ Min=30000,Max=50000, Level=4, Route="4" },
                new LevelConfig(){ Min=50000,Max=decimal.MaxValue, Level=5, Route="5" },
            } },
            {"业务招待",new List<LevelConfig>(){
                new LevelConfig(){ Min=0,Max=2000, Level=1, Route="1" },
                new LevelConfig(){ Min=2000,Max=10000, Level=3, Route="3" },
                new LevelConfig(){ Min=10000,Max=50000, Level=4, Route="4" },
                new LevelConfig(){ Min=50000,Max=decimal.MaxValue, Level=5, Route="5" },
            } },
            {"日常办公",new List<LevelConfig>(){
                new LevelConfig(){ Min=0,Max=2000, Level=1, Route="1" },
                new LevelConfig(){ Min=2000,Max=10000, Level=3, Route="3" },
                new LevelConfig(){ Min=10000,Max=50000, Level=4, Route="4" },
                new LevelConfig(){ Min=50000,Max=decimal.MaxValue, Level=5, Route="5" },
            } },
            {"固定资产",new List<LevelConfig>(){
                new LevelConfig(){ Min=0,Max=2000, Level=1, Route="1" },
                new LevelConfig(){ Min=2000,Max=30000, Level=3, Route="3" },
                new LevelConfig(){ Min=30000,Max=50000, Level=4, Route="4" },
                new LevelConfig(){ Min=50000,Max=decimal.MaxValue, Level=5, Route="5" },
            } },
            {"电话通讯费",new List<LevelConfig>(){
                new LevelConfig(){ Min=0,Max=decimal.MaxValue, Level=1, Route="1" },
            } },
            {"水电租赁",new List<LevelConfig>(){
                new LevelConfig(){ Min=0,Max=decimal.MaxValue, Level=2, Route="2" },
            } },
            {"广告",new List<LevelConfig>(){
                new LevelConfig(){ Min=0,Max=30000, Level=1,Route="1" },
                new LevelConfig(){ Min=30000,Max=50000, Level=3,Route="3" },
                new LevelConfig(){ Min=50000,Max=decimal.MaxValue, Level=5,Route="5" },
            } },
            {"退客户",new List<LevelConfig>(){
                new LevelConfig(){ Min=0,Max=50000, Level=3,Route="3" },
                new LevelConfig(){ Min=50000,Max=100000, Level=4,Route="4" },
                new LevelConfig(){ Min=100000,Max=decimal.MaxValue, Level=5,Route="5" },

            } },
            {"员工差旅",new List<LevelConfig>(){
                new LevelConfig(){ Min=0,Max=2000, Level=1,Route="1" },
                new LevelConfig(){ Min=2000,Max=30000, Level=3,Route="3" },
                new LevelConfig(){ Min=30000,Max=50000, Level=4,Route="4" },
                new LevelConfig(){ Min=50000,Max=decimal.MaxValue, Level=5,Route="5" },

            } },
            {"其他",new List<LevelConfig>(){
                new LevelConfig(){ Min=0,Max=30000,Level=1,Route="1" },
                new LevelConfig(){ Min=30000,Max=decimal.MaxValue,Level=5,Route="5" },
            } },
        };

        class LevelConfig
        {
            public decimal Min { get; set; }

            public decimal Max { get; set; }

            public string Route { get; set; }

            public int Level { get; set; }
        }
    }

    /// <summary>
    /// 报销主表
    /// </summary>
    [DisplayName("报销主表")]
    public class SrbBxMainFormService : CRUDDataSource, IFlowFormService
    {
        public override int Insert(IDictionary<string, object> data)
        {
            data["approvestatus"] = 0;
            return base.Insert(data);
        }

        public override int Update(string key, IDictionary<string, object> data)
        {
            var main = data;
            var sum = data["totalamount"].ConvertTo<decimal>();
            var clear = data["cleanborrow"].ConvertTo<decimal>();
            if (main.ContainsKey("cleanborrow") && main["cleanborrow"] != null)
            {
                decimal.TryParse(main["cleanborrow"].ToString(), out clear);
            }
            var delta = clear - sum;
            main["totalamount"] = sum;
            if (delta > 0)
            {
                main["repaytocompay"] = delta;
                main["paymenttoempo"] = 0m;
            }
            else
            {
                main["repaytocompay"] = 0m;
                main["paymenttoempo"] = -delta;
            }
            data["approvestatus"] = 1;
            return base.Update(key, data);
        }

        public bool CanRunFlow()
        {
            return true;
        }

        public void OnFlowFinish(string instanceId, string dataLocator, bool agree, string denyOption)
        {
            var data = this.Get(dataLocator);
            data["approvestatus"] = agree ? 2 : 3;
            data["ts"] = DateTime.Now;
            base.Update(dataLocator, data);
        }

        public IDictionary<string, object> SetParameter()
        {
            throw new NotImplementedException();
        }
    }

    [Table("srb_bx_detail")]
    public class BxDetailEntity : EntityBase
    {
        public string bxDetailid { get; set; }

        public string bxid { get; set; }

        public DateTime date { get; set; }

        public string description { get; set; }

        public decimal travelexpense { get; set; }

        public decimal quarterage { get; set; }

        public decimal tablemoney { get; set; }

        public decimal transportmoney { get; set; }

        public decimal entertainment { get; set; }

        public decimal communication { get; set; }

        public decimal maintenance { get; set; }

        public decimal rollcharge { get; set; }

        public decimal gasoline { get; set; }

        public decimal officesupplies { get; set; }

        public decimal activity { get; set; }

        public decimal other { get; set; }


        private decimal _amount = decimal.MinValue;

        public decimal amount
        {
            get
            {
                if (_amount == decimal.MinValue)
                {
                    var ft = FastType.Get(this.GetType());
                    decimal result = 0;
                    foreach (var g in ft.Getters)
                    {
                        if (g.Name.Equals("amount") || g.Info.PropertyType != typeof(decimal))
                        {
                            continue;
                        }
                        result += g.GetValue(this).ConvertTo<decimal>();
                    }
                    _amount = result;
                }
                return _amount;
            }
            set
            {
                _amount = value;
            }
        }

        public DateTime ts { get; set; }

        public decimal fixassets { get; set; }

        public decimal waterele { get; set; }

        public decimal ads { get; set; }

        public decimal giveback { get; set; }
    }
}
