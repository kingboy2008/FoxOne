/*********************************************************
 * 作　　者：刘海峰
 * 联系邮箱：mailTo:liuhf@FoxOne.net
 * 创建时间：2015/6/8 15:29:41
 * 描述说明：
 * *******************************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using log4net;
using System.Configuration;
using log4net.Config;
using System.IO;

namespace FoxOne.Core
{
    public static class Logger
    {
        private const string CONFIG_FILE_NAME = "log4net.config";
        private const string CONFIG_SECTION_NAME = "log4net";

        static Logger()
        {
            Configure();
        }

        private static void Configure()
        {
            if (null != ConfigurationManager.GetSection(CONFIG_SECTION_NAME))
            {
                XmlConfigurator.Configure();
            }
            else
            {
                FileInfo file;
                if (Utility.FindConfigFile(CONFIG_FILE_NAME, out file))
                {
                    XmlConfigurator.Configure(file);
                }
            }
        }

        public static void Info(string message)
        {
            //Swj.Infrastructure.Tools.Logging.LogHelper.Info(message);
            LogManager.GetLogger(typeof(Logger)).Info(message);
        }

        public static void Debug(string message)
        {
            //Swj.Infrastructure.Tools.Logging.LogHelper.Debug(message);
            LogManager.GetLogger(typeof(Logger)).Debug(message);
        }

        public static void Debug(string format,params object[] args)
        {
            Debug(format.FormatTo(args));
        }

        public static void Info(string format,params object[] args)
        {
            Info(format.FormatTo(args));
        }

        public static void Error(string message, Exception ex)
        {
            //Swj.Infrastructure.Tools.Logging.LogHelper.Error(message, ex);
            LogManager.GetLogger(typeof(Logger)).Error(message, ex);
            
        }

    }
}
