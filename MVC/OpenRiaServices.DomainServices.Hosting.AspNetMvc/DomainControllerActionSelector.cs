// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Web.Http;
using System.Web.Http.Controllers;
using OpenRiaServices.DomainServices.Server;

namespace OpenRiaServices.DomainServices.Hosing.AspNetMvc
{
    internal sealed class DomainControllerActionSelector : ApiControllerActionSelector
    {
        private const string ActionRouteKey = "action";
        private const string SubmitActionValue = "Submit";

        public override HttpActionDescriptor SelectAction(HttpControllerContext controllerContext)
        {
            // first check to see if this is a call to Submit
            if (controllerContext.RouteData.Values.TryGetValue(ActionRouteKey, out object routeAction)
                && routeAction is string actionName
                && actionName.Equals(SubmitActionValue, StringComparison.Ordinal))
            {
                return new SubmitActionDescriptor(controllerContext.ControllerDescriptor, controllerContext.Controller.GetType());
            }


            // TODO: Try to implement this
            // next check to see if this is a direct invocation of a CUD action
            /*var description = DomainServiceDescription.GetDescription(controllerContext.ControllerDescriptor.ControllerType);
            var op = description.DomainOperationEntries.FirstOrDefault(op => op.Name == actionName));
            if (op != null)
            {
                switch (op.Operation)
                {
                    case DomainOperation.Query:
                        return new 
                }
            }*/

            /*UpdateActionDescriptor action = description.GetUpdateAction(actionName);
            if (action != null)
            {
                return new SubmitProxyActionDescriptor(action);
            }
            */

            return base.SelectAction(controllerContext);
        }
    }
}
