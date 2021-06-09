using OpenRiaServices.Client;
using OpenRiaServices.Client.HttpDomainClient;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace WPFCore
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Web client 
            DomainContext.DomainClientFactory = new BinaryHttpDomainClientFactory()
            {
                //HttpMessageHandler = new HttpClientHandler()
                //{
                //    UseProxy = true,
                //    AutomaticDecompression = System.Net.DecompressionMethods.Deflate | System.Net.DecompressionMethods.GZip
                //},
                ServerBaseUri = new Uri("https://localhost:44300/", UriKind.Absolute)
            };
        }
    }
}
