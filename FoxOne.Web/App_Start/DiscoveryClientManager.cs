using Steeltoe.Discovery.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace FoxOne.Web
{
    public class DiscoveryClientManager
    {
        public static IDiscoveryClient DiscoveryClient { get; private set; }

        static DiscoveryClientManager()
        {
            ServerConfig.RegisterConfig();
        }

        public static void InitializeAndStart()
        {
            DiscoveryOptions configOptions = new DiscoveryOptions(ServerConfig.Configuration)
            {
                ClientType = DiscoveryClientType.EUREKA
            };
            DiscoveryClientFactory factory = new DiscoveryClientFactory(configOptions);
            DiscoveryClient = (IDiscoveryClient)factory.CreateClient();
        }

        public static async void Shutdown()
        {
            if (DiscoveryClient != null)
            {
                await DiscoveryClient.ShutdownAsync();
            }
        }
    }
}