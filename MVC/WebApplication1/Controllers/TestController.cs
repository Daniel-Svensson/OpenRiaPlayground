using OpenRiaServices.DomainServices.Hosing.AspNetMvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.OData;
using System.Web.Mvc;

namespace WebApplication1.Controllers
{
    [DataContract]
    public class Example
    {
        [DataMember]
        public int Id { get; set; }

        [DataMember]
        public string Value { get; set; }
    }

   

    public class TestController : DomainController
    {
        Example[] data = new Example[] { new Example { Id = 1, Value = "value1" }
            , new Example {Id= 2, Value = "value2" } };

        // GET api/<controller>
        [EnableQuery]
        public async Task<IQueryable<Example>> GetExamples()
        {
            await Task.Delay(5);
            return data.AsQueryable();
        }

        // GET api/<controller>/5
        public Example GetExample(int id)
        {
            return data.FirstOrDefault(e => e.Id == id);
        }

    }
}