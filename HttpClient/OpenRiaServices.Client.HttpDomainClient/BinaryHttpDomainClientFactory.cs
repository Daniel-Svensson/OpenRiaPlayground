using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;

namespace OpenRiaServices.Client.HttpDomainClient
{
    public class BinaryHttpDomainClientFactory
        : DomainClientFactory
    {
        public BinaryHttpDomainClientFactory()
            : this(new HttpClientHandler()
            {
                CookieContainer = new System.Net.CookieContainer(),
                UseCookies = true, 
                 AutomaticDecompression = System.Net.DecompressionMethods.Deflate | System.Net.DecompressionMethods.GZip,
            })
        {
        }

        public BinaryHttpDomainClientFactory(HttpMessageHandler messageHandler)
        {
            HttpMessageHandler = messageHandler;
        }

        protected override DomainClient CreateDomainClientCore(Type serviceContract, Uri serviceUri, bool requiresSecureEndpoint)
        {
            var httpClient = new HttpClient(HttpMessageHandler, disposeHandler: false)
            {
                BaseAddress = new Uri(serviceUri.AbsoluteUri + "/binary/", UriKind.Absolute),
            };

            return new BinaryHttpDomainClient(httpClient, serviceContract);
        }

        public HttpMessageHandler HttpMessageHandler { get; set; }
    }
}
