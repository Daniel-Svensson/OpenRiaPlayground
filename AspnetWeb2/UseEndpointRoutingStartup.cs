
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using AspnetWeb2.Services;
using Microsoft.AspNetCore.Routing.Internal;
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

        services.AddTransient<ServerSideAsyncDomainService>();
        services.AddSingleton<FrameworkEndpointDataSource>();
    }

    public void Configure(IApplicationBuilder app)
    {
        app.UseStaticFiles();
        app.UseRouting();
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
                    sb.AppendLine("Endpoints:");
                    foreach (var endpoint in dataSource.Endpoints.OfType<RouteEndpoint>().OrderBy(e => e.RoutePattern.RawText, StringComparer.OrdinalIgnoreCase))
                    {
                        sb.AppendLine(FormattableString.Invariant($"- <a href=\"{endpoint.RoutePattern.RawText}\">{endpoint.RoutePattern.RawText}</a>"));
                        foreach (var metadata in endpoint.Metadata)
                        {
                            sb.AppendLine("    " + metadata);
                        }
                    }

                    var response = httpContext.Response;
                    response.StatusCode = 200;

                    sb.AppendLine("</body></html>");
                    response.ContentType = "text/html";
                    return response.WriteAsync(sb.ToString());
                });
            endpoints.MapGet(
                "/plaintext",
                (httpContext) =>
                {
                    var response = httpContext.Response;
                    var payloadLength = _plainTextPayload.Length;
                    response.StatusCode = 200;
                    response.ContentType = "text/plain";
                    response.ContentLength = payloadLength;
                    return response.Body.WriteAsync(_plainTextPayload, 0, payloadLength);
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

            endpoints.MapGet("/attributes", HandlerWithAttributes);

            endpoints.Map("/getwithattributes", Handler);

            endpoints.MapFramework(frameworkBuilder =>
            {
                frameworkBuilder.AddPattern("/transform/{hub=TestHub}/{method=TestMethod}");
                frameworkBuilder.AddPattern("/{hub}/{method=TestMethod}");

                frameworkBuilder.AddHubMethod("TestHub", "TestMethod", context => context.Response.WriteAsync("TestMethod!"));
                frameworkBuilder.AddHubMethod("Login", "Authenticate", context => context.Response.WriteAsync("Authenticate!"));
                frameworkBuilder.AddHubMethod("Login", "Logout", context => context.Response.WriteAsync("Logout!"));

                //frameworkBuilder.AddDomainService("WeatherForecastService", typeof(AspnetWeb2.Services.WeatherForecastService));
                frameworkBuilder.AddDomainService(typeof(AspnetWeb2.Services.WeatherForecastService));
                frameworkBuilder.AddDomainService(typeof(TestDomainServices.ServerSideAsyncDomainService));
            });
        });

    }

    [Authorize]
    private Task HandlerWithAttributes(HttpContext context)
    {
        return context.Response.WriteAsync("I have ann authorize attribute");
    }

    [HttpGet]
    private Task Handler(HttpContext context)
    {
        return context.Response.WriteAsync("I have a method metadata attribute");
    }

    private class AuthorizeAttribute : Attribute
    {

    }

    private class HttpGetAttribute : Attribute, IHttpMethodMetadata
    {
        public bool AcceptCorsPreflight => false;

        public IReadOnlyList<string> HttpMethods { get; } = new List<string> { "GET" };
    }
}
