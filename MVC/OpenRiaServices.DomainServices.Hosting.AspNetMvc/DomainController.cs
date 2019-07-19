using System;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;
using System.Web.Http.OData;
using System.Web.Http.OData.Query;
using OpenRiaServices.DomainServices.Server;

namespace OpenRiaServices.DomainServices.Hosing.AspNetMvc
{
    [DomainControllerConfigurationAttribute]
    public class DomainController : DomainService, IHttpController
    {
        //        Task<HttpResponseMessage> ExecuteAsync(HttpControllerContext controllerContext, CancellationToken cancellationToken);
        private bool _initialized;
        private HttpActionContext _actionContext;
        static private readonly DomainControllerActionSelector s_actionSelector = new DomainControllerActionSelector();
 //       static private readonly DomainControllerActionInvoker s_Invoker = new DomainControllerActionInvoker();

        public HttpControllerContext ControllerContext { get; private set; }

        public HttpActionContext ActionContext
        {
            get
            {
                return _actionContext;
            }
            set
            {
                if (value == null)
                {
                    throw System.Web.Http.Error.PropertyNull();
                }
                _actionContext = value;
            }
        }

        public async Task<HttpResponseMessage> ExecuteAsync(HttpControllerContext controllerContext, CancellationToken cancellationToken)
        {
            // TODO: Validate only registered once?
            var request = controllerContext.Request;
            if (request != null)
            {
                request.RegisterForDispose(this);
            }
            HttpControllerDescriptor controllerDescriptor = controllerContext.ControllerDescriptor;
            ServicesContainer services = controllerDescriptor.Configuration.Services;
            HttpActionDescriptor httpActionDescriptor = s_actionSelector.SelectAction(controllerContext);

            var binder = (IActionValueBinder)services.GetService(typeof(IActionValueBinder));
            var binding = binder.GetBinding(httpActionDescriptor);

            ActionContext = new HttpActionContext(controllerContext, httpActionDescriptor);

            // Parses method arguments
            await binding.ExecuteBindingAsync(ActionContext, cancellationToken);

            Initialize(controllerContext);

            // Invoker handlers result conversion
            var result = await httpActionDescriptor.ExecuteAsync(controllerContext, ActionContext.ActionArguments, cancellationToken);

            // Format result
            return httpActionDescriptor.ResultConverter.Convert(controllerContext, result);
        }


        //
        // Summary:
        //     Initializes the System.Web.Http.ApiController instance with the specified controllerContext.
        //
        // Parameters:
        //   controllerContext:
        //     The System.Web.Http.Controllers.HttpControllerContext object that is used for
        //     the initialization.
        protected virtual void Initialize(HttpControllerContext controllerContext)
        {
            if (_initialized)
                throw System.Web.Http.Error.InvalidOperation("already initialized");

            if (controllerContext == null)
            {
                throw System.Web.Http.Error.ArgumentNull(nameof(controllerContext));
            }
            _initialized = true;
            ControllerContext = controllerContext;
            // TODO: Resolve type first
            // TODO: Improve perf
            var operationType = DomainOperationType.Query;
            var context = new DomainServiceContext(new ServiceContainerWrapper(controllerContext), operationType);
            base.Initialize(context);
        }

        class ServiceContainerWrapper : IServiceContainer
        {
            private readonly HttpControllerContext _httpControllerContext;

            public ServiceContainerWrapper(HttpControllerContext httpControllerContext)
            {
                this._httpControllerContext = httpControllerContext;
            }

            void IServiceContainer.AddService(Type serviceType, object serviceInstance)
            {
                throw new NotImplementedException();
            }

            void IServiceContainer.AddService(Type serviceType, object serviceInstance, bool promote)
            {
                throw new NotImplementedException();
            }

            void IServiceContainer.AddService(Type serviceType, ServiceCreatorCallback callback)
            {
                throw new NotImplementedException();
            }

            void IServiceContainer.AddService(Type serviceType, ServiceCreatorCallback callback, bool promote)
            {
                throw new NotImplementedException();
            }

            object IServiceProvider.GetService(Type serviceType)
            {
                if (serviceType == typeof(IPrincipal))
                    return _httpControllerContext.RequestContext.Principal;
                else
                    return _httpControllerContext.Configuration.Services.GetService(serviceType);
            }

            void IServiceContainer.RemoveService(Type serviceType)
            {
                throw new NotImplementedException();
            }

            void IServiceContainer.RemoveService(Type serviceType, bool promote)
            {
                throw new NotImplementedException();
            }
        }


    }
}
