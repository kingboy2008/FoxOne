using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace FoxOne.Web
{
    public class ServerConfig
    {
        public static IConfigurationRoot Configuration { get; set; }

        public static void RegisterConfig()
        {
            string basePath = HttpContext.Current.Server.MapPath("~/App_Config");
            var builder = new ConfigurationBuilder()
                .SetBasePath(basePath)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

            Configuration = builder.Build();
        }
    }
}