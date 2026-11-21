using System.Globalization;
using System.Security.Claims;
using FCSalesTool.Api.Auth;
using FCSalesTool.Infrastructure.Identity;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace FCSalesTool.Api.Tests;

/// <summary>
/// Stands in for the EVE SSO round trip: mints the external cookie the OAuth handler sets after
/// a successful callback, then follows the redirect to <c>/auth/eve/complete</c>.
/// </summary>
internal static class EveSsoSimulator
{
    public static async Task<HttpResponseMessage> CompleteAsync(
        this HttpClient client, ApiFactory factory, EveIdentity character, string intent = "login", string? returnUrl = null, string? startedByUserId = null)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, character.CharacterId.ToString(CultureInfo.InvariantCulture)),
            new(ClaimTypes.Name, character.Name),
        };
        if (character.OwnerHash is not null)
        {
            claims.Add(new Claim(EveClaims.OwnerHash, character.OwnerHash));
        }

        var items = new Dictionary<string, string?> { ["intent"] = intent, ["returnUrl"] = returnUrl };
        if (startedByUserId is not null)
        {
            items["userId"] = startedByUserId;
        }

        var options = factory.Services.GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>()
            .Get(IdentityConstants.ExternalScheme);
        var ticket = new AuthenticationTicket(
            new ClaimsPrincipal(new ClaimsIdentity(claims, EveSsoOptions.Scheme)),
            new AuthenticationProperties(items),
            IdentityConstants.ExternalScheme);

        using var request = new HttpRequestMessage(HttpMethod.Get, "/auth/eve/complete");
        request.Headers.Add("Cookie", $"{options.Cookie.Name}={options.TicketDataFormat.Protect(ticket)}");
        return await client.SendAsync(request, TestApi.Ct);
    }
}
