using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FCSalesTool.Api.Account;

namespace FCSalesTool.Api.Tests;

internal static class TestApi
{
    public const string Password = "correct horse battery";

    public static CancellationToken Ct => TestContext.Current.CancellationToken;

    public static string NewEmail() => $"pilot-{Guid.NewGuid():N}@example.com";

    public static Task<HttpResponseMessage> Post(this HttpClient client, string path, object body) =>
        client.PostAsJsonAsync(path, body, Ct);

    /// <summary>Registers and confirms an account, then signs in with it.</summary>
    public static async Task<MeResponse> SignUpAndSignIn(this HttpClient client, ApiFactory factory, string email, string password = Password)
    {
        Assert.Equal(HttpStatusCode.Accepted, (await client.Post("/api/auth/register", new { email, password })).StatusCode);
        var (_, query) = factory.Email.LatestLink(email);
        var confirm = await client.Post("/api/auth/confirm-email", new { userId = query["userId"], code = query["code"] });
        Assert.Equal(HttpStatusCode.NoContent, confirm.StatusCode);

        var login = await client.Post("/api/auth/login", new { email, password, rememberMe = false });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        return (await login.Content.ReadFromJsonAsync<MeResponse>(Ct))!;
    }

    public static async Task<string?> ProblemCode(this HttpResponseMessage response)
    {
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(Ct));
        return doc.RootElement.TryGetProperty("code", out var code) ? code.GetString() : null;
    }

    public static async Task<string[]> FieldErrors(this HttpResponseMessage response, string field)
    {
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(Ct));
        return doc.RootElement.GetProperty("errors").TryGetProperty(field, out var errors)
            ? errors.EnumerateArray().Select(e => e.GetString()!).ToArray()
            : [];
    }
}
