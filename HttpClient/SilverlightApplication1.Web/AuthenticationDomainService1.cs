using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using OpenRiaServices.Server;
using OpenRiaServices.Server.Authentication.AspNetMembership;

namespace SilverlightApplication1.Web
{
    [EnableClientAccess]
    public class AuthenticationDomainService1 : AuthenticationBase<User>
    {
        // To enable Forms/Windows Authentication for the Web Application, edit the appropriate section of web.config file.
        protected override bool ValidateUser(string userName, string password)
        {
            return userName == "test";
            // return base.ValidateUser(userName, password);
        }

        protected override void IssueAuthenticationToken(System.Security.Principal.IPrincipal principal, bool isPersistent)
        {
            
            base.IssueAuthenticationToken(principal, isPersistent);
        }

    }

    public class User : UserBase
    {
        // NOTE: Profile properties can be added here 
        // To enable profiles, edit the appropriate section of web.config file.

        // public string MyProfileProperty { get; set; }
    }
}