using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;

namespace OpenRiaServices.DomainServices.Client.Web
{
    /// <summary>
    /// Creates <see cref="WebDomainClient{TContract}"/> instances
    /// </summary>
    public class SoapDomainClientFactory : WcfDomainClientFactory
    {
        private readonly SoapQueryBehavior _queryBehaviors;

        public SoapDomainClientFactory()
        {
            _queryBehaviors = new SoapQueryBehavior(this);
        }

        protected override ChannelFactory<TContract> CreateChannelFactory<TContract>(Uri endpoint, bool requiresSecureEndpoint)
        {
            var factory = base.CreateChannelFactory<TContract>(endpoint, requiresSecureEndpoint);

            try
            {
                factory.Endpoint.Behaviors.Add(_queryBehaviors);
                return factory;
            }
            catch
            {
                ((IDisposable)factory)?.Dispose();
                throw;
            }
        }

        protected override EndpointAddress CreateEndpointAddress(Uri endpoint, bool requiresSecureEndpoint)
        {
            return new EndpointAddress(new Uri(endpoint.OriginalString + "/soap", UriKind.Absolute));
        }

        protected override Binding CreateBinding(Uri endpoint, bool requiresSecureEndpoint)
        {
            BasicHttpBinding binding = new BasicHttpBinding();
            if (endpoint.Scheme == Uri.UriSchemeHttps)
            {
                binding.Security.Mode = BasicHttpSecurityMode.Transport;
            }
            binding.MaxReceivedMessageSize = int.MaxValue;
            binding.AllowCookies = CookieContainer != null;
            return binding;
        }
    }

    /// <summary>
    /// A SOAP endpoint behavior which injects a message inspector that adds query headers.
    /// </summary>
    sealed class SoapQueryBehavior : IEndpointBehavior
    {
        private readonly SoapQueryInspector _inspector;
        
        public SoapQueryBehavior(WcfDomainClientFactory factory)
        {
            _inspector = new SoapQueryInspector(factory);
        }

        /// <summary>
        /// Implement to pass data at runtime to bindings to support custom behavior.
        /// </summary>
        /// <param name="endpoint">The endpoint to modify.</param>
        /// <param name="bindingParameters">The objects that binding elements require to support the behavior.</param>
        void IEndpointBehavior.AddBindingParameters(ServiceEndpoint endpoint, BindingParameterCollection bindingParameters)
        {
            // Method intentionally left empty.
        }

        /// <summary>
        /// Implements a modification or extension of the client across an endpoint.
        /// </summary>
        /// <param name="endpoint">The endpoint that is to be customized.</param>
        /// <param name="clientRuntime">The client runtime to be customized.</param>
        void IEndpointBehavior.ApplyClientBehavior(ServiceEndpoint endpoint, ClientRuntime clientRuntime)
        {
            clientRuntime.MessageInspectors.Add(_inspector);
        }

        /// <summary>
        /// Implements a modification or extension of the service across an endpoint.
        /// </summary>
        /// <param name="endpoint">The endpoint that exposes the contract.</param>
        /// <param name="endpointDispatcher">The endpoint dispatcher to be modified or extended.</param>
        void IEndpointBehavior.ApplyDispatchBehavior(ServiceEndpoint endpoint, EndpointDispatcher endpointDispatcher)
        {
            // Method intentionally left empty.
        }

        /// <summary>
        /// Implement to confirm that the endpoint meets some intended criteria.
        /// </summary>
        /// <param name="endpoint">The endpoint to validate.</param>
        void IEndpointBehavior.Validate(ServiceEndpoint endpoint)
        {
            // Method intentionally left empty.
        }
    }

    sealed class SoapQueryInspector : IClientMessageInspector
    {
        private const string IncludeTotalCountPropertyName = "DomainServiceIncludeTotalCount";
        private const string QueryPropertyName = "DomainServiceQuery";

        private readonly WcfDomainClientFactory _factory;

        public SoapQueryInspector(WcfDomainClientFactory factory)
        {
            _factory = factory;
        }

        public CookieContainer CookieContainer { get; set; }

        public object AfterReceiveRequest(ref Message request, IClientChannel channel, InstanceContext instanceContext)
        {
            return null;
        }

        public static string RewriteAction(string action)
        {
            return action.Insert(action.LastIndexOf("/"), "soap");
        }

        public void BeforeSendReply(ref Message reply, object correlationState)
        {
            // Method intentionally left empty.
        }

        public object BeforeSendRequest(ref Message request, IClientChannel channel)
        {
            // make sure the channel uses the shared cookie container
            channel.GetProperty<IHttpCookieContainerManager>().CookieContainer =
                _factory.CookieContainer;

            request.Headers.Action = RewriteAction(request.Headers.Action);

            // Check for Query Options
            if (OperationContext.Current != null)
            {
                object queryProperty;
                object includeTotalCountProperty;

                OperationContext.Current.OutgoingMessageProperties.TryGetValue(QueryPropertyName, out queryProperty);
                OperationContext.Current.OutgoingMessageProperties.TryGetValue(IncludeTotalCountPropertyName, out includeTotalCountProperty);

                // Add Query Options header if any options were specified
                if (queryProperty != null || includeTotalCountProperty != null)
                {
                    var queryParts = (queryProperty != null) ? QuerySerializer.Serialize((IQueryable)queryProperty) : null;
                    var includeTotalCount = (bool?)includeTotalCountProperty;

                    // Prepare the request message copy to be modifie
                    var header = new QueryOptionsHeader(queryParts, includeTotalCount == true);
                    request.Headers.Add(header);
                }
            }

            return null;
        }

        public void AfterReceiveReply(ref Message reply, object correlationState)
        {
            // Method intentionally left empty.
        }
    }
}


namespace OpenRiaServices.DomainServices.Client
{
    internal class QueryOptionsHeader : MessageHeader
    {
        private const string QueryHeaderName = "DomainServiceQuery";
        private const string QueryHeaderNamespace = "DomainServices";
        readonly IEnumerable<ServiceQueryPart> _queryParts;
        readonly bool _includeTotalCount;

        public QueryOptionsHeader(IEnumerable<ServiceQueryPart> queryParts, bool includeTotalCount)
        {
            _queryParts = queryParts;
            _includeTotalCount = includeTotalCount;
        }

        public override string Name
        {
            get { return QueryHeaderName; }
        }

        public override string Namespace
        {
            get { return QueryHeaderNamespace; }
        }

        private const string QueryOptionElementName = "QueryOption";
        private const string QueryNameAttribute = "Name";
        private const string QueryValueAttribute = "Value";
        private const string QueryIncludeTotalCountOption = "includeTotalCount";

        protected override void OnWriteHeaderContents(System.Xml.XmlDictionaryWriter writer, MessageVersion messageVersion)
        {
            if (_queryParts != null)
            {
                foreach (var part in _queryParts)
                {
                    writer.WriteStartElement(QueryOptionElementName);
                    writer.WriteAttributeString(QueryNameAttribute, part.QueryOperator);
                    writer.WriteAttributeString(QueryValueAttribute, part.Expression);
                    writer.WriteEndElement();
                }
            }

            if (_includeTotalCount == true)
            {
                writer.WriteStartElement(QueryOptionElementName);
                writer.WriteAttributeString(QueryNameAttribute, QueryIncludeTotalCountOption);
                writer.WriteAttributeString(QueryValueAttribute, "true");
                writer.WriteEndElement();
            }
        }
    }
}