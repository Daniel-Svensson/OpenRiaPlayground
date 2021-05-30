using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Web;
using OpenRiaServices.Tools;
using OpenRiaServices.Tools.TextTemplate;
using OpenRiaServices.Tools.TextTemplate.CSharpGenerators;

namespace SilverlightApplication1.Web
{
    /*
     [DomainServiceClientCodeGenerator("My", "C#")]
    public class CodeGenerator : CSharpClientCodeGenerator 
    {
        protected override EntityGenerator EntityGenerator
        {
            get { return new MyEntityGenerator(); }
        }

        protected override DomainContextGenerator DomainContextGenerator
        {
            get
            {
                return new MyCtxGen();
            }
        }
    }
    
    public class MyEntityGenerator : CSharpEntityGenerator
    {
        public override string TransformText()
        {
            base.WriteLine("// USING NEW CODE GEN !!!");
            return base.TransformText();
        }

        protected override void GenerateClassDeclaration()
        {
            base.GenerateClassDeclaration();
        }

        protected override void GenerateProperty(PropertyDescriptor propertyDescriptor)
        {
            base.GenerateProperty(propertyDescriptor);
        }
    }

    public class MyCtxGen : CSharpDomainContextGenerator
    {
        protected override string GenerateDomainContextClass()
        {
            base.WriteLine("// GenerateDomainContextClass CALLED for {0} ", DomainServiceDescription.DomainServiceType);
            return base.GenerateDomainContextClass();
        }
    }
     */
}