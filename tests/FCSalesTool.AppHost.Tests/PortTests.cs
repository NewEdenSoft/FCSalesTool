using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace FCSalesTool.AppHost.Tests;

/// <summary>
/// Local ports must not drift: the EVE SSO app is registered with
/// http://localhost:5173/auth/eve/callback. Builds the AppHost model only (no containers start).
/// </summary>
public sealed class PortTests
{
    [Fact]
    public async Task Endpoints_use_the_fixed_ports()
    {
        var endpoints = await EndpointsAsync();

        Assert.Equal(
            [
                "api:http:5257",
                "api:https:7102",
                "mailpit:http:8025",
                "web:http:5173",
            ],
            endpoints
                .Where(e => e.Resource != "postgres" && e.Name != "smtp")
                .Select(e => $"{e.Resource}:{e.Name}:{e.Port}")
                .Order());
    }

    [Fact]
    public async Task Web_has_a_single_http_endpoint_for_the_sso_callback()
    {
        // WithEndpoint("http", ...) creates a new endpoint if the name is wrong; make sure it
        // configured the Vite server's own endpoint instead.
        var web = (await EndpointsAsync()).Where(e => e.Resource == "web").ToList();

        var http = Assert.Single(web);
        Assert.Equal(("http", 5173), (http.Scheme, http.Port));
    }

    private static async Task<List<(string Resource, string Name, string Scheme, int? Port)>> EndpointsAsync()
    {
        var ct = TestContext.Current.CancellationToken;
        var builder = await DistributedApplicationTestingBuilder.CreateAsync<Projects.FCSalesTool_AppHost>(ct);
        await using var app = await builder.BuildAsync(ct);
        var model = app.Services.GetRequiredService<DistributedApplicationModel>();

        return model.Resources
            .SelectMany(r => r.Annotations.OfType<EndpointAnnotation>().Select(e => (r.Name, e.Name, e.UriScheme, e.Port)))
            .ToList();
    }
}
