// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace RoutingSandbox;

public class Program
{
    public const string EndpointRoutingScenario = "endpointrouting";
    public const string RouterScenario = "router";

    public static Task Main(string[] args)
    {
        var host = GetHostBuilder(args).Build();
        return host.RunAsync();
    }

    // For unit testing
    public static IHostBuilder GetHostBuilder(string[] args)
    {
        return new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                    .UseKestrel()
                    .UseIISIntegration()
                    .UseContentRoot(Environment.CurrentDirectory)
                    .UseStartup<UseEndpointRoutingStartup>();
            })
            .ConfigureLogging(b =>
            {
                b.AddConsole();
                b.SetMinimumLevel(LogLevel.Warning);
            });
    }
}