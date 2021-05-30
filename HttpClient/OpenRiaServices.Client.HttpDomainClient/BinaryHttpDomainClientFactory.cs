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
                UseCookies = true
            })
        {
        }

        public BinaryHttpDomainClientFactory(HttpMessageHandler messageHandler)
        {
            HttpMessageHandler = messageHandler;
        }

        protected override DomainClient CreateDomainClientCore(Type serviceContract, Uri serviceUri, bool requiresSecureEndpoint)
        {
            return new BinaryHttpDomainClient(serviceContract, serviceUri, HttpMessageHandler);
        }

        public HttpMessageHandler HttpMessageHandler { get; set; }
    }
}
