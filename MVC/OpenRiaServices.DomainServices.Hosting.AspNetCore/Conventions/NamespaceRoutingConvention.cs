using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Internal;
using System.Linq;
using System.Text;

namespace OpenRiaServices.DomainServices.Hosing.AspNetMvc
{

    //public class ApplicationModel : DefaultApplicationModelProvider
    //{
    //}

    public class ApplicationProviderModel : IApplicationModelProvider
    {
        public int Order => throw new System.NotImplementedException();

        public void OnProvidersExecuted(ApplicationModelProviderContext context)
        {
            throw new System.NotImplementedException();
        }

        public void OnProvidersExecuting(ApplicationModelProviderContext context)
        {
        }
    }

    public class NamespaceRoutingConvention : IControllerModelConvention
    {
        private readonly string _baseNamespace;

        public NamespaceRoutingConvention(string baseNamespace)
        {
            _baseNamespace = baseNamespace;
        }

        public void Apply(ControllerModel controller)
        {
            var hasRouteAttributes = controller.Selectors.Any(selector =>
                                                    selector.AttributeRouteModel != null);
            if (hasRouteAttributes)
            {
                // This controller manually defined some routes, so treat this 
                // as an override and not apply the convention here.
                return;
            }

            // Use the namespace and controller name to infer a route for the controller.
            //
            // Example:
            //
            //  controller.ControllerTypeInfo ->    "My.Application.Admin.UsersController"
            //  baseNamespace ->                    "My.Application"
            //
            //  template =>                         "Admin/[controller]"
            //
            // This makes your routes roughly line up with the folder structure of your project.
            //
            var namespc = controller.ControllerType.Namespace;
            if (namespc == null)
                return;
            var template = new StringBuilder();
            template.Append(namespc, _baseNamespace.Length + 1,
                            namespc.Length - _baseNamespace.Length - 1);
            template.Replace('.', '/');
            template.Append("/[controller]");

            foreach (var selector in controller.Selectors)
            {
                selector.AttributeRouteModel = new AttributeRouteModel()
                {
                    Template = template.ToString()
                };
            }
        }
    }
}
