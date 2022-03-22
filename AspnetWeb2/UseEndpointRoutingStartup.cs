
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using AspnetWeb2.Services;
using Microsoft.AspNetCore.Routing.Internal;
using OpenRiaServices.Client.Benchmarks.Server.Cities;
using System.Text;
using TestDomainServices;

public class UseEndpointRoutingStartup
{
    private static readonly byte[] _plainTextPayload = Encoding.UTF8.GetBytes("Plain text!");

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddRouting(options =>
        {
            //options.ConstraintMap["slugify"] = typeof(SlugifyParameterTransformer);
        });

        services.AddTransient<WeatherForecastService>();

        services.AddTransient<OpenRiaServices.Client.Benchmarks.Server.Cities.CityDomainService>();
        services.AddTransient<ServerSideAsyncDomainService>();
        services.AddOpenRiaServices();
    }

    public void Configure(IApplicationBuilder app)
    {
        app.UseStaticFiles();
        //app.UseAuthentication();
        app.UseRouting();
        //app.UseAuthorization();
        app.UseEndpoints(endpoints =>
        {
         //   endpoints.MapHello("/helloworld", "World");

            endpoints.MapGet(
                "/",
                (httpContext) =>
                {
                    var dataSource = httpContext.RequestServices.GetRequiredService<EndpointDataSource>();

                    var sb = new StringBuilder();
                    sb.Append("<html><body>");
                    sb.AppendLine("<p>Endpoints:</p>");
                    foreach (var endpoint in dataSource.Endpoints.OfType<RouteEndpoint>().OrderBy(e => e.RoutePattern.RawText, StringComparer.OrdinalIgnoreCase))
                    {
                        sb.AppendLine(FormattableString.Invariant($"- <a href=\"{endpoint.RoutePattern.RawText}\">{endpoint.RoutePattern.RawText}</a><br />"));
                        foreach (var metadata in endpoint.Metadata)
                        {
                            sb.AppendLine("<li>" + metadata + "</li>");
                        }
                    }

                    var response = httpContext.Response;
                    response.StatusCode = 200;

                    sb.AppendLine("</body></html>");
                    response.ContentType = "text/html";
                    return response.WriteAsync(sb.ToString());
                });

            endpoints.MapGet(
                "/graph",
                (httpContext) =>
                {
                    using (var writer = new StreamWriter(httpContext.Response.Body, Encoding.UTF8, 1024, leaveOpen: true))
                    {
                        var graphWriter = httpContext.RequestServices.GetRequiredService<DfaGraphWriter>();
                        var dataSource = httpContext.RequestServices.GetRequiredService<EndpointDataSource>();
                        graphWriter.Write(dataSource, writer);
                    }

                    return Task.CompletedTask;
                }).WithDisplayName("DFA Graph");

             endpoints.MapOpenRiaServices(frameworkBuilder =>
            {

                CityDomainService.GetCitiesResult = CreateValidCities(1).ToList();

                //frameworkBuilder.AddDomainService("WeatherForecastService", typeof(AspnetWeb2.Services.WeatherForecastService));
                frameworkBuilder.AddDomainService(typeof(AspnetWeb2.Services.WeatherForecastService));
                frameworkBuilder.AddDomainService(typeof(TestDomainServices.ServerSideAsyncDomainService));
                frameworkBuilder.AddDomainService(typeof(OpenRiaServices.Client.Benchmarks.Server.Cities.CityDomainService));
            });
        });

    }

    public static IEnumerable<OpenRiaServices.Client.Benchmarks.Server.Cities.City> CreateValidCities(int num)
    {
        for (var i = 0; i < num; i++)
        {
            yield return new OpenRiaServices.Client.Benchmarks.Server.Cities.City { Name = "Name" + ToAlphaKey(i), CountyName = "Country", StateName = "SA" };
        }
    }
    public static string ToAlphaKey(int num)
    {
        var sb = new StringBuilder();
        do
        {
            var alpha = (char)('a' + (num % 25));
            sb.Append(alpha);
            num /= 25;
        } while (num > 0);

        return sb.ToString();
    }
}
