using System;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Net.Http;
using System.Security.Claims;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OpenRiaServices.DomainServices.Server;

namespace OpenRiaServices.DomainServices.Hosing.AspNetMvc
{
    //[Route("Services/[controller]/[action]")]
    // TODO: ActionFilters for query, invoke and submit
    // Convention to prevent actions from "Ignore" / non data service methods
    [Controller]
    public abstract class DomainController : DomainService
    {
        private ControllerContext _controllerContext;

        //
        // Summary:
        //     /// Gets or sets the Microsoft.AspNetCore.Mvc.ControllerContext. ///
        //
        // Remarks:
        //     /// Microsoft.AspNetCore.Mvc.Controllers.IControllerActivator activates this
        //     property while activating controllers. /// If user code directly instantiates
        //     a controller, the getter returns an empty /// Microsoft.AspNetCore.Mvc.ControllerContext.
        //     ///
        [ControllerContext]
        public ControllerContext ControllerContext
        {
            get
            {
                if (_controllerContext == null)
                {
                    _controllerContext = new ControllerContext();
                }
                return _controllerContext;
            }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }
                _controllerContext = value;
            }
        }


        public HttpContext HttpContext => ControllerContext.HttpContext;
        public ClaimsPrincipal User => HttpContext?.User;
    }
}
