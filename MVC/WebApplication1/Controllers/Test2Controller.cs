using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.OData;

namespace WebApplication1.Controllers
{
    public class Test2Controller : ApiController
    {
        private static readonly Example[] data = new Example[] { new Example { Id = 1, Value = "value1" }
            , new Example {Id= 2, Value = "value2" } };

        // GET api/<controller>
        [EnableQuery]
        //[Route("GetExamples")]
        public IQueryable<Example> GetExamples()
        {
          //  await Task.Delay(5);
            return data.AsQueryable();
        }
        /*
        public async Task<IQueryable<Example>> GetExamples()
        {
            await Task.Delay(5);
            return data.AsQueryable();
        }
        */
        //// GET api/<controller>/5
        //public Example GetExample(int id)
        //{
        //    return data.FirstOrDefault(e => e.Id == id);
        //}

    }
}
