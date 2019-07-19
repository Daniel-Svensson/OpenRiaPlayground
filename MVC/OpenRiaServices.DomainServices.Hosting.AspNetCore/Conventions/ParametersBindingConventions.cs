using System;
using System.Collections.Generic;
using System.Text;

namespace OpenRiaServices.DomainServices.Hosting.AspNetCore.Conventions
{
    using System;
    using Microsoft.AspNetCore.Mvc.ApplicationModels;
    using Microsoft.AspNetCore.Mvc.ModelBinding;

    namespace AppModelSample.Conventions
    {
        public class ParametersBindingConventions : IParameterModelConvention
        {
            void IParameterModelConvention.Apply(ParameterModel parameter)
            {
                if (parameter.BindingInfo == null)
                {
                    parameter.BindingInfo = new BindingInfo();
                }
                parameter.BindingInfo.BindingSource = BindingSource.Query;
            }
        }
    }
}
