// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Routing.Patterns;
using OpenRiaServices.Server;

public class FrameworkConfigurationBuilder
{
    private readonly FrameworkEndpointDataSource _dataSource;

    internal FrameworkConfigurationBuilder(FrameworkEndpointDataSource dataSource)
    {
        _dataSource = dataSource;
    }

    public void AddPattern(string pattern)
    {
        AddPattern(RoutePatternFactory.Parse(pattern));
    }

    public void AddPattern(RoutePattern pattern)
    {
        _dataSource.Patterns.Add(pattern);
    }

    public void AddHubMethod(string hub, string method, RequestDelegate requestDelegate)
    {
        _dataSource.HubMethods.Add(new HubMethod
        {
            Hub = hub,
            Method = method,
            RequestDelegate = requestDelegate
        });
    }

    internal void AddDomainService(Type type)
    {
        var longName = type.FullName.Replace('.', '-') + ".svc";
        DomainServiceDescription description = DomainServiceDescription.GetDescription(type);

        _dataSource.DomainServices.Add(longName, description);
        _dataSource.DomainServices.Add(type.Name, description);
    }

    internal void AddDomainService(string name, Type type)
    {
        throw new NotImplementedException();
    }
}
