using System.Security.Claims;
using System.Threading.RateLimiting;
using FCSalesTool.Api.Common;
using FCSalesTool.Infrastructure.Identity;
using FCSalesTool.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;

namespace FCSalesTool.Api.Auth;

public static class AuthSetup
{
    /// <summary>Rate-limit policy for anonymous auth endpoints (login, register, emails).</summary>
    public const string RateLimitPolicy = "auth";

    public static WebApplicationBuilder AddAppAuth(this WebApplicationBuilder builder)
    {
        var services = builder.Services;
        var config = builder.Configuration;

        // In dev the browser talks to the Vite server over http; production is https only.
        var cookieSecurity = builder.Environment.IsDevelopment() ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;

        services.Configure<AppOptions>(config.GetSection(AppOptions.Section));
        services.Configure<EveSsoOptions>(config.GetSection(EveSsoOptions.Section));
        services.AddHttpContextAccessor();
        services.AddScoped<AppUrls>();
        services.AddSingleton<EveTokenValidator>();

        // The Vite dev proxy (and a production reverse proxy) forward the browser's host/scheme.
        services.Configure<ForwardedHeadersOptions>(o =>
            o.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost);

        services.AddDataProtection()
            .SetApplicationName("OmegaWatch")
            .PersistKeysToDbContext<AppDbContext>();

        services.AddIdentityCore<AppUser>(o =>
            {
                o.User.RequireUniqueEmail = false; // EVE-only accounts have no email; see AppDbContext.
                o.SignIn.RequireConfirmedEmail = true;
                // Length over composition rules (NIST SP 800-63B).
                o.Password.RequiredLength = 10;
                o.Password.RequireDigit = false;
                o.Password.RequireLowercase = false;
                o.Password.RequireUppercase = false;
                o.Password.RequireNonAlphanumeric = false;
                o.Lockout.AllowedForNewUsers = true;
                o.Lockout.MaxFailedAccessAttempts = 5;
                o.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            })
            .AddEntityFrameworkStores<AppDbContext>()
            .AddSignInManager()
            .AddDefaultTokenProviders();

        var auth = services.AddAuthentication(IdentityConstants.ApplicationScheme);
        auth.AddIdentityCookies(cookies =>
        {
            cookies.ApplicationCookie!.Configure(o =>
            {
                o.Cookie.Name = "ow.auth";
                o.Cookie.HttpOnly = true;
                o.Cookie.SameSite = SameSiteMode.Lax;
                o.Cookie.SecurePolicy = cookieSecurity;
                o.ExpireTimeSpan = TimeSpan.FromDays(14);
                o.SlidingExpiration = true;
                // SPA + API: answer with status codes, never redirect to a login page.
                o.Events.OnRedirectToLogin = context => SetStatus(context, StatusCodes.Status401Unauthorized);
                o.Events.OnRedirectToAccessDenied = context => SetStatus(context, StatusCodes.Status403Forbidden);
            });
            cookies.ExternalCookie!.Configure(o =>
            {
                o.Cookie.Name = "ow.external";
                o.Cookie.SameSite = SameSiteMode.Lax;
                o.Cookie.SecurePolicy = cookieSecurity;
                o.ExpireTimeSpan = TimeSpan.FromMinutes(10);
            });
        });

        var eve = config.GetSection(EveSsoOptions.Section).Get<EveSsoOptions>() ?? new EveSsoOptions();
        if (eve.IsConfigured)
        {
            auth.AddOAuth(EveSsoOptions.Scheme, "EVE Online", o =>
            {
                o.ClientId = eve.ClientId!;
                o.ClientSecret = eve.ClientSecret!;
                o.AuthorizationEndpoint = eve.AuthorizationEndpoint;
                o.TokenEndpoint = eve.TokenEndpoint;
                o.CallbackPath = "/auth/eve/callback";
                o.SignInScheme = IdentityConstants.ExternalScheme;
                o.SaveTokens = false; // We only need to know who the character is.
                foreach (var scope in (eve.Scopes ?? string.Empty).Split(' ', StringSplitOptions.RemoveEmptyEntries))
                {
                    o.Scope.Add(scope);
                }

                // The callback is a top-level GET from login.eveonline.com, so Lax is enough.
                o.CorrelationCookie.SameSite = SameSiteMode.Lax;
                o.CorrelationCookie.SecurePolicy = cookieSecurity;

                o.Events.OnRedirectToAuthorizationEndpoint = context =>
                {
                    context.Response.Redirect(EveTokenValidator.RemoveEmptyScope(context.RedirectUri));
                    return Task.CompletedTask;
                };
                o.Events.OnCreatingTicket = async context =>
                {
                    var validator = context.HttpContext.RequestServices.GetRequiredService<EveTokenValidator>();
                    var character = await validator.ValidateAsync(context.AccessToken!, context.HttpContext.RequestAborted);
                    context.Identity!.AddClaim(new Claim(ClaimTypes.NameIdentifier, character.CharacterId.ToString(System.Globalization.CultureInfo.InvariantCulture)));
                    context.Identity.AddClaim(new Claim(ClaimTypes.Name, character.Name));
                    if (character.OwnerHash is not null)
                    {
                        context.Identity.AddClaim(new Claim(EveClaims.OwnerHash, character.OwnerHash));
                    }
                };
                o.Events.OnRemoteFailure = context =>
                {
                    var isLink = context.Properties?.Items.TryGetValue(AuthEndpoints.IntentKey, out var intent) == true
                        && intent == AuthEndpoints.IntentLink;
                    context.Response.Redirect(isLink ? "/account?error=eve_failed" : "/login?error=eve_failed");
                    context.HandleResponse();
                    return Task.CompletedTask;
                };
            });
        }

        services.AddAuthorization();

        var permitsPerMinute = config.GetValue("Auth:RateLimitPerMinute", 20);
        services.AddRateLimiter(o =>
        {
            o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            o.AddPolicy(RateLimitPolicy, context => RateLimitPartition.GetFixedWindowLimiter(
                context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions { PermitLimit = permitsPerMinute, Window = TimeSpan.FromMinutes(1) }));
        });

        return builder;
    }

    private static Task SetStatus(RedirectContext<CookieAuthenticationOptions> context, int status)
    {
        context.Response.StatusCode = status;
        return Task.CompletedTask;
    }
}
