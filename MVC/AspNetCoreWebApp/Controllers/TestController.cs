using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;
//using Microsoft.AspNet.OData;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OpenRiaServices.DomainServices.Hosing.AspNetMvc;

namespace AspNetCoreWebApp.Controllers
{

    [DataContract]
    public class Example
    {
        [DataMember]
        public int Id { get; set; }

        [DataMember]
        public string Value { get; set; }
    }

    [Route("api/[controller]/[action]")]
 //   [ApiController]
    public class TestController : DomainController
    {
        Example[] data = new Example[] { new Example { Id = 1, Value = "value1" }
            , new Example {Id= 2, Value = "value2" } };

        // GET api/<controller>
 //       [EnableQuery]
        public IQueryable<Example> GetExamples()
        {
            return data.AsQueryable();
        }

        // GET api/<controller>/5
        public Example GetExample(int id)
        {
            return data.FirstOrDefault(e => e.Id == id);
        }

    }
}