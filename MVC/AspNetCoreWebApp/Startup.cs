using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
//using AspNetCore.RouteAnalyzer;
//using Microsoft.AspNet.OData.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Swashbuckle.AspNetCore.Swagger;

namespace AspNetCoreWebApp
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();
            //  services.AddRazorPages();
            //services.AddOData();

            //services.AddRouteAnalyzer();

            // Register the Swagger generator, defining 1 or more Swagger documents
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo { Title = "My API", Version = "v1" });
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app,
            IHostingEnvironment env
            )
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseRouting();


            // Enable middleware to serve generated Swagger as a JSON endpoint.
            app.UseSwagger();

            // Enable middleware to serve swagger-ui (HTML, JS, CSS, etc.), 
            // specifying the Swagger JSON endpoint.
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1");
            });



            app.UseAuthentication();
            app.UseAuthorization();
 //           app.UseAuthorization(new AuthorizationPolicyBuilder().Build()));
            //             app.UseCors();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}")
                ;
                endpoints.MapControllerRoute("ria", "Services/{controller}/{action}");
                endpoints.MapFallback(ROUTE_FALLBACK);
                //endpoints.MapRazorPages();

                foreach(var a in endpoints.DataSources)
                {
                    foreach(var b in a.Endpoints)
                    {
                        Debug.WriteLine($"Endpint {b.DisplayName} {b.Metadata}");
                    }
                }
 //               endpoints.MapRouteAnalyzer("/routes");
            });

            //app.EnableDependencyInjection();


            // Add this block
            //applicationLifetime.ApplicationStarted.Register(() =>
            //{
            //    var infos = routeAnalyzer.GetAllRouteInformations();
            //    Debug.WriteLine("======== ALL ROUTE INFORMATION ========");
            //    foreach (var info in infos)
            //    {
            //        Debug.WriteLine(info.ToString());
            //    }
            //    Debug.WriteLine("");
            //    Debug.WriteLine("");
            //});
        }

        private Task ROUTE_FALLBACK(HttpContext httpContext)
        {
            // get endpoint context that is set in Endpoint Routing middleware
            var endpointSelectorContext = httpContext.Features.Get<IEndpointFeature>() as EndpointSelectorContext;
            var routeData = httpContext.GetRouteData();

            foreach(var router in routeData.Routers)
            {
            }

            return Task.CompletedTask;

        }
    }
}
