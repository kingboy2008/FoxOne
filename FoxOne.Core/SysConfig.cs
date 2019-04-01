using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FoxOne.Core
{
    public static class SysConfig
    {

        static SysConfig()
        {
            AppSettings = new AppSettingPropery();
            SystemTitle = AppSettings["SystemTitle"];
            CopyRightName = AppSettings["CopyRightName"];
            Assemblies = AppSettings["Assemblies"];
            SystemStatus = AppSettings["SystemStatus"];
            SuperAdminRoleName = AppSettings["SuperAdminRoleName"];
            DefaultUserRole = AppSettings["DefaultUserRole"];
            DomainName = AppSettings["DomainName"];
        }

        public static string ExtFieldName = "_ExtField_";

        public static AppSettingPropery AppSettings { get; private set; }

        public static string SystemTitle { get; private set; }

        public static string CopyRightName { get; private set; }

        public static string SystemVersion { get; private set; }

        /// <summary>
        /// 系统当前状态：Develop,Test,Run
        /// </summary>
        public static string SystemStatus { get; private set; }


        public const string PKLength = "38";

        public static bool IsProductEnv
        {
            get
            {
                return SystemStatus.Equals("Run", StringComparison.OrdinalIgnoreCase);
            }
        }


        public static string[] DayOfWeekCN = new string[] { "星期日", "星期一", "星期二", "星期三", "星期四", "星期五", "星期六" };

        public static string Assemblies { get; private set; }

        public static string IconBasePath { get { return "/images/icons/"; } }

        public static string ControlImageBasePath { get { return "/images/Controls/"; } }

        public static string SuperAdminRoleName { get; private set; }

        public static string DefaultUserRole { get; private set; }

        public static string DomainName { get; private set; }
    }

    public class AppSettingPropery : Dictionary<string, string>
    {
        private AppSettingsReader _reader;
        private AppSettingsReader Reader
        {
            get
            {
                return _reader ?? (_reader = new AppSettingsReader());
            }
        }

        public new string this[string key]
        {
            get
            {
                if (!base.Keys.Contains(key))
                {
                    base[key] = Reader.GetValue(key, typeof(string)) as string;
                }
                return base[key];
            }
            set
            {
                base[key] = value;
            }
        }
    }
}
