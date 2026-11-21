using System.Net;
using System.Net.Http.Json;

namespace FCSalesTool.Api.Tests;

public sealed class StatusEndpointTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    [Fact]
    public async Task GetStatus_ReturnsOk()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/status", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<StatusResponse>(TestContext.Current.CancellationToken);
        Assert.Equal("ok", body?.Status);
    }
}
