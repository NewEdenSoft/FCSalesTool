using System.Security.Claims;
using FCSalesTool.Domain.Accounts;
using FCSalesTool.Infrastructure.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace FCSalesTool.Api.Auth;

public sealed class EveSsoOptions
{
    public const string Section = "Auth:Eve";
    public const string Scheme = "EVE";

    public string? ClientId { get; set; }

    public string? ClientSecret { get; set; }

    /// <summary>Space-separated ESI scopes. Empty = authentication only (we only need to know who the character is).</summary>
    public string? Scopes { get; set; }

    public string AuthorizationEndpoint { get; set; } = "https://login.eveonline.com/v2/oauth/authorize";

    public string TokenEndpoint { get; set; } = "https://login.eveonline.com/v2/oauth/token";

    public string MetadataAddress { get; set; } = "https://login.eveonline.com/.well-known/oauth-authorization-server";

    public bool IsConfigured => !string.IsNullOrEmpty(ClientId) && !string.IsNullOrEmpty(ClientSecret);
}

/// <summary>Claim types we put on the short-lived external cookie after EVE SSO.</summary>
public static class EveClaims
{
    public const string OwnerHash = "eve:owner";

    public static EveIdentity? ToIdentity(ClaimsPrincipal principal)
    {
        var id = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        var name = principal.FindFirstValue(ClaimTypes.Name);
        return long.TryParse(id, out var characterId) && !string.IsNullOrEmpty(name)
            ? new EveIdentity(characterId, name, principal.FindFirstValue(OwnerHash))
            : null;
    }
}

/// <summary>
/// Validates the EVE SSO access token (a JWT) against the SSO's published signing keys and
/// extracts the character. The token itself is discarded afterwards.
/// </summary>
public sealed class EveTokenValidator
{
    // EVE docs: the issuer may be the host name or the URI; check all forms.
    private static readonly string[] Issuers = ["login.eveonline.com", "https://login.eveonline.com", "https://login.eveonline.com/"];
    private const string EveAudience = "EVE Online";

    private readonly EveSsoOptions _options;
    private readonly ConfigurationManager<OpenIdConnectConfiguration> _metadata;
    private readonly JsonWebTokenHandler _handler = new();

    public EveTokenValidator(IOptions<EveSsoOptions> options)
    {
        _options = options.Value;
        _metadata = new ConfigurationManager<OpenIdConnectConfiguration>(
            _options.MetadataAddress,
            new OpenIdConnectConfigurationRetriever(),
            new HttpDocumentRetriever { RequireHttps = true });
    }

    public async Task<EveIdentity> ValidateAsync(string accessToken, CancellationToken ct)
    {
        var result = await ValidateCoreAsync(accessToken, ct);
        if (result.Exception is SecurityTokenSignatureKeyNotFoundException)
        {
            // Key rollover: refresh the cached keys once.
            _metadata.RequestRefresh();
            result = await ValidateCoreAsync(accessToken, ct);
        }

        if (!result.IsValid)
        {
            throw new SecurityTokenValidationException("EVE SSO token is invalid.", result.Exception);
        }

        var token = (JsonWebToken)result.SecurityToken;
        // Both the client id and the static "EVE Online" value must be present.
        if (!token.Audiences.Contains(_options.ClientId) || !token.Audiences.Contains(EveAudience))
        {
            throw new SecurityTokenInvalidAudienceException("EVE SSO token audience is invalid.");
        }

        if (!EveSubject.TryParseCharacterId(token.Subject, out var characterId)
            || !token.TryGetPayloadValue<string>("name", out var name)
            || string.IsNullOrWhiteSpace(name))
        {
            throw new SecurityTokenValidationException("EVE SSO token has no character.");
        }

        token.TryGetPayloadValue<string>("owner", out var owner);
        return new EveIdentity(characterId, name, string.IsNullOrEmpty(owner) ? null : owner);
    }

    private async Task<TokenValidationResult> ValidateCoreAsync(string accessToken, CancellationToken ct)
    {
        var metadata = await _metadata.GetConfigurationAsync(ct);
        return await _handler.ValidateTokenAsync(accessToken, new TokenValidationParameters
        {
            ValidIssuers = Issuers,
            ValidAudiences = [_options.ClientId!, EveAudience],
            IssuerSigningKeys = metadata.SigningKeys,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1),
        });
    }

    /// <summary>
    /// The OAuth handler always sends <c>scope=</c>. EVE's docs don't say an empty value is
    /// accepted, so for authentication-only logins the parameter is left out.
    /// </summary>
    public static string RemoveEmptyScope(string authorizationUrl)
    {
        var queryStart = authorizationUrl.IndexOf('?');
        if (queryStart < 0)
        {
            return authorizationUrl;
        }

        var query = QueryHelpers.ParseQuery(authorizationUrl[queryStart..]);
        if (!query.TryGetValue("scope", out var scope) || !string.IsNullOrWhiteSpace(scope))
        {
            return authorizationUrl;
        }

        query.Remove("scope");
        return QueryHelpers.AddQueryString(authorizationUrl[..queryStart], query);
    }
}
