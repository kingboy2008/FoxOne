using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StackExchange.Redis;

namespace FoxOne.Core
{
    public class RedisCache : ICache
    {        
        /// <summary>
        /// redis系统前缀, Swj开头+各自系统简称  （防止系统缓存覆盖）
        /// </summary>
        private static string _keyPrefix = "FoxOne:{0}_".FormatTo( RedisManager.Manager.ClientName);

        public RedisCache()
        {
            var key = AllKeys;
        }

        public IList<string> AllKeys
        {
            get
            {
                var allkeys= RedisManager.Manager.GetDatabase().Execute("keys", "*");
                if (allkeys.IsNull)
                {
                    return null;
                }
                return ((string[])allkeys).Where(c => c.StartsWith(_keyPrefix)).ToList();
            }
        }

        public void Clean()
        {
            var allKey = AllKeys;
            var keys = new RedisKey[allKey.Count];

            for (var i = 0; i < allKey.Count; i++)
            {
                keys[i] = allKey[i];
            }

            try
            {
                RedisManager.Manager.GetDatabase().KeyDelete(keys);
            }
            catch (Exception ex)
            {
                Logger.Error("RedisCache Clean", ex);
            }
        }

        public object GetValue(string key)
        {
            try
            {
                return RedisManager.Manager.GetDatabase().StringGet(_keyPrefix + key);
            }
            catch (Exception ex)
            {
                Logger.Error("RedisCache GetValue", ex);
                return null;
            }
        }

        public void Remove(string key)
        {
            try
            {
                RedisManager.Manager.GetDatabase().KeyDelete(_keyPrefix+key);
            }
            catch (Exception ex)
            {
                Logger.Error("RedisCache Remove", ex);
            }
        }

        public void SetValue(string key, object value, DateTime absoluteExpiration, TimeSpan slidingExpiration)
        {
            try
            {
                RedisManager.Manager.GetDatabase().StringSet(key, value.ToString());

            }
            catch (Exception ex)
            {
                Logger.Error("RedisCache SetValue", ex);
            }
        }
    }


    /// <summary>
    /// Redis连接管理对象
    /// </summary>
    public class RedisManager
    {
        private static ConnectionMultiplexer _redis;
        private static object _locker = new object();

        /// <summary>
        /// Redis管对象
        /// </summary>
        public static ConnectionMultiplexer Manager
        {
            get
            {
                if (_redis == null)
                {
                    lock (_locker)
                    {
                        if (_redis != null) return _redis;
                        _redis = lazyConnection.Value;
                        return _redis;
                    }
                }

                return _redis;
            }
        }

        /// <summary>
        /// 获取Redis连接对象
        /// </summary>
        private static Lazy<ConnectionMultiplexer> lazyConnection = new Lazy<ConnectionMultiplexer>(() =>
        {
            ConfigurationOptions configurationOptions = new ConfigurationOptions()
            {
                CommandMap = CommandMap.Create(new HashSet<string>()
                             {
                                 "INFO",
                                 "CONFIG",
                                 "CLUSTER",
                                 "PING",
                                 "ECHO",
                                 "CLIENT"
                             }, available: false),
                KeepAlive = 180,
                DefaultVersion = new Version(2, 8, 24),
                AllowAdmin = false,
                ClientName = SysConfig.AppSettings["SysCode"],
                ResponseTimeout = 5000,
                ConnectTimeout = 30000,
                ConnectRetry = 10,
                WriteBuffer = 10240
            };

            //动态配置Reids服务器后期可横向扩展
            //string[] endPointsList = _redisProvider.RedisConnect.Split(',');
            string[] endPointsList = SysConfig.AppSettings["RedisConnect"].Split(',');
            foreach (string item in endPointsList)
            {
                configurationOptions.EndPoints.Add(item);
            }

            var connect = ConnectionMultiplexer.Connect(configurationOptions);
            connect.ConnectionFailed += MuxerConnectionFailed;
            connect.ErrorMessage += MuxerErrorMessage;
            return connect;
        });
        

        #region 事件

        /// <summary>
        /// Redis发生错误
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void MuxerErrorMessage(object sender, RedisErrorEventArgs e)
        {
            Logger.Info("RedisCache ErrorMessage {0}", e.Message);
        }

        /// <summary>
        /// Redis连接失败,如果重新连接成功你将不会收到这个通知
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void MuxerConnectionFailed(object sender, ConnectionFailedEventArgs e)
        {
            Logger.Info("重新连接：Endpoint failed: {0},{1}{2}", e.EndPoint, e.FailureType, e.Exception == null ? "" : (", " + e.Exception.Message));
        }

        #endregion 事件
    }
    
}
