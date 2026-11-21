using System.Security.Claims;
using FCSalesTool.Api.Account;
using FCSalesTool.Api.Common;
using FCSalesTool.Domain.Accounts;
using FCSalesTool.Infrastructure.Identity;
using FCSalesTool.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;

namespace FCSalesTool.Api.Auth;

/// <summary>
/// Sign-in flows. <c>/auth/eve/*</c> are browser navigations (redirects to and from EVE SSO);
/// <c>/api/auth/*</c> are JSON endpoints for email + password accounts.
/// </summary>
public static class AuthEndpoints
{
    internal const string IntentKey = "intent";
    internal const string IntentLink = "link";
    private const string ReturnUrlKey = "returnUrl";
    private const string UserIdKey = "userId";

    public static void MapAuthEndpoints(this WebApplication app)
    {
        var eve = app.MapGroup("/auth/eve").ExcludeFromDescription();
        eve.MapGet("/login", EveLogin);
        eve.MapGet("/link", EveLink).RequireAuthorization();
        eve.MapGet("/complete", EveComplete);

        var api = app.MapGroup("/api/auth").WithTags("Auth");
        api.MapGet("/providers", (IOptions<EveSsoOptions> eveOptions) =>
            TypedResults.Ok(new AuthProvidersResponse(eveOptions.Value.IsConfigured)));
        api.MapPost("/register", Register).RequireRateLimiting(AuthSetup.RateLimitPolicy);
        api.MapPost("/login", Login).RequireRateLimiting(AuthSetup.RateLimitPolicy);
        api.MapPost("/logout", Logout);
        api.MapPost("/confirm-email", ConfirmEmail).RequireRateLimiting(AuthSetup.RateLimitPolicy);
        api.MapPost("/confirm-email-change", ConfirmEmailChange).RequireRateLimiting(AuthSetup.RateLimitPolicy);
        api.MapPost("/resend-confirmation", ResendConfirmation).RequireRateLimiting(AuthSetup.RateLimitPolicy);
        api.MapPost("/forgot-password", ForgotPassword).RequireRateLimiting(AuthSetup.RateLimitPolicy);
        api.MapPost("/reset-password", ResetPassword).RequireRateLimiting(AuthSetup.RateLimitPolicy);
    }

    // ---- EVE SSO (browser navigation) ----

    private static IResult EveLogin(string? returnUrl, IOptions<EveSsoOptions> options) =>
        !options.Value.IsConfigured
            ? TypedResults.Redirect("/login?error=eve_unavailable")
            : TypedResults.Challenge(Properties("login", returnUrl), [EveSsoOptions.Scheme]);

    private static IResult EveLink(string? returnUrl, ClaimsPrincipal user, UserManager<AppUser> users, IOptions<EveSsoOptions> options)
    {
        if (!options.Value.IsConfigured)
        {
            return TypedResults.Redirect("/account?error=eve_unavailable");
        }

        var properties = Properties(IntentLink, returnUrl ?? "/account");
        properties.Items[UserIdKey] = users.GetUserId(user);
        return TypedResults.Challenge(properties, [EveSsoOptions.Scheme]);
    }

    private static AuthenticationProperties Properties(string intent, string? returnUrl) => new(new Dictionary<string, string?>
    {
        [IntentKey] = intent,
        [ReturnUrlKey] = LocalUrl.Sanitize(returnUrl),
    })
    {
        RedirectUri = "/auth/eve/complete",
    };

    /// <summary>EVE SSO has succeeded; turn the character into a local account session.</summary>
    private static async Task<RedirectHttpResult> EveComplete(
        HttpContext context,
        EveAccountService accounts,
        UserManager<AppUser> users,
        SignInManager<AppUser> signIn,
        CancellationToken ct)
    {
        var external = await context.AuthenticateAsync(IdentityConstants.ExternalScheme);
        await context.SignOutAsync(IdentityConstants.ExternalScheme);

        var character = external.Succeeded ? EveClaims.ToIdentity(external.Principal) : null;
        IDictionary<string, string?> items = external.Properties?.Items ?? new Dictionary<string, string?>();
        var returnUrl = LocalUrl.Sanitize(Item(items, ReturnUrlKey));
        var isLink = Item(items, IntentKey) == IntentLink;

        if (character is null)
        {
            return TypedResults.Redirect(isLink ? "/account?error=eve_failed" : "/login?error=eve_failed");
        }

        if (isLink)
        {
            // The account must be the one that started the link, and still be signed in.
            var startedBy = Item(items, UserIdKey);
            var currentUserId = users.GetUserId(context.User);
            if (startedBy is null || startedBy != currentUserId || !Guid.TryParse(currentUserId, out var userId))
            {
                return TypedResults.Redirect("/login?error=eve_link_session");
            }

            var outcome = await accounts.LinkAsync(userId, character, ct);
            var target = returnUrl ?? "/account";
            return TypedResults.Redirect(outcome switch
            {
                EveLinkOutcome.Linked or EveLinkOutcome.AlreadyLinked => QueryHelpers.AddQueryString(target, "linked", character.CharacterId.ToString(System.Globalization.CultureInfo.InvariantCulture)),
                EveLinkOutcome.LinkedToOtherAccount => QueryHelpers.AddQueryString(target, "error", "eve_linked_elsewhere"),
                _ => "/login?error=eve_link_session",
            });
        }

        var (user, _) = await accounts.SignInAsync(character, ct);
        await signIn.SignInAsync(user, isPersistent: true, authenticationMethod: EveSsoOptions.Scheme);
        return TypedResults.Redirect(returnUrl ?? "/");
    }

    // ---- Email + password (JSON) ----

    private static async Task<Results<Accepted, ValidationProblem>> Register(
        RegisterRequest request,
        UserManager<AppUser> users,
        AccountMailer mailer,
        TimeProvider time,
        CancellationToken ct)
    {
        if (!AccountInput.TryNormalizeEmail(request.Email, out var email))
        {
            return Problems.Field("email", "Enter a valid email address.");
        }

        var displayName = DisplayName.FromEmail(email);
        if (!string.IsNullOrWhiteSpace(request.DisplayName))
        {
            if (!DisplayName.TryNormalize(request.DisplayName, out displayName, out var nameError))
            {
                return Problems.Field("displayName", nameError!);
            }
        }

        var user = new AppUser { Id = Guid.CreateVersion7(), Email = email, DisplayName = displayName, CreatedAt = time.GetUtcNow() };
        user.UserName = user.Id.ToString("N");

        // Check the password before looking the email up, so the response doesn't reveal
        // whether an account exists.
        var passwordCheck = await AccountInput.ValidatePasswordAsync(users, user, request.Password);
        if (!passwordCheck.Succeeded)
        {
            return Problems.FromIdentity(passwordCheck, "password");
        }

        var existing = await users.FindByEmailAsync(email);
        if (existing is not null)
        {
            if (existing.EmailConfirmed)
            {
                await mailer.SendAlreadyRegisteredAsync(existing, ct);
            }
            else
            {
                await mailer.SendConfirmationAsync(existing, ct);
            }

            return TypedResults.Accepted((string?)null);
        }

        var created = await users.CreateAsync(user, request.Password);
        if (!created.Succeeded)
        {
            return Problems.FromIdentity(created, "password");
        }

        await mailer.SendConfirmationAsync(user, ct);
        return TypedResults.Accepted((string?)null);
    }

    private static async Task<Results<Ok<MeResponse>, ProblemHttpResult>> Login(
        LoginRequest request,
        UserManager<AppUser> users,
        SignInManager<AppUser> signIn,
        AppDbContext db,
        CancellationToken ct)
    {
        var invalid = Problems.Coded(StatusCodes.Status401Unauthorized, "invalid_credentials", "Email or password is incorrect.");
        var user = AccountInput.TryNormalizeEmail(request.Email, out var email) ? await users.FindByEmailAsync(email) : null;
        if (user is null || string.IsNullOrEmpty(request.Password))
        {
            return invalid;
        }

        var result = await signIn.PasswordSignInAsync(user, request.Password, request.RememberMe, lockoutOnFailure: true);
        if (result.Succeeded)
        {
            return TypedResults.Ok(await db.ToMeAsync(user, ct));
        }

        if (result.IsLockedOut)
        {
            return Problems.Coded(StatusCodes.Status403Forbidden, "locked_out", "Too many failed attempts.", "Try again in 15 minutes, or reset your password.");
        }

        if (result.IsNotAllowed)
        {
            // Identity reports "not allowed" before checking the password; only say the email
            // is unconfirmed to someone who knows the password.
            if (!await users.CheckPasswordAsync(user, request.Password))
            {
                await users.AccessFailedAsync(user);
                return invalid;
            }

            return Problems.Coded(StatusCodes.Status403Forbidden, "email_not_confirmed", "Confirm your email first.", "We sent you a link when you registered. You can ask for a new one.");
        }

        return invalid;
    }

    private static async Task<NoContent> Logout(SignInManager<AppUser> signIn)
    {
        await signIn.SignOutAsync();
        return TypedResults.NoContent();
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> ConfirmEmail(ConfirmEmailRequest request, UserManager<AppUser> users)
    {
        var user = await users.FindByIdAsync(request.UserId.ToString());
        var token = AccountInput.DecodeToken(request.Code);
        if (user is null || token is null)
        {
            return InvalidLink();
        }

        if (user.EmailConfirmed)
        {
            return TypedResults.NoContent();
        }

        var result = await users.ConfirmEmailAsync(user, token);
        return result.Succeeded ? TypedResults.NoContent() : InvalidLink();
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> ConfirmEmailChange(
        ConfirmEmailChangeRequest request,
        HttpContext context,
        UserManager<AppUser> users,
        SignInManager<AppUser> signIn)
    {
        var user = await users.FindByIdAsync(request.UserId.ToString());
        var token = AccountInput.DecodeToken(request.Code);
        if (user is null || token is null || !AccountInput.TryNormalizeEmail(request.Email, out var email))
        {
            return InvalidLink();
        }

        var owner = await users.FindByEmailAsync(email);
        if (owner is not null && owner.Id != user.Id)
        {
            return Problems.Coded(StatusCodes.Status409Conflict, "email_taken", "That email is already used by another account.");
        }

        if (owner?.Id == user.Id && user.EmailConfirmed)
        {
            return TypedResults.NoContent();
        }

        var result = await users.ChangeEmailAsync(user, email, token);
        if (!result.Succeeded)
        {
            return InvalidLink();
        }

        // The security stamp changed; keep the current browser signed in.
        if (users.GetUserId(context.User) == user.Id.ToString())
        {
            await signIn.RefreshSignInAsync(user);
        }

        return TypedResults.NoContent();
    }

    private static async Task<Accepted> ResendConfirmation(EmailRequest request, UserManager<AppUser> users, AccountMailer mailer, CancellationToken ct)
    {
        var user = AccountInput.TryNormalizeEmail(request.Email, out var email) ? await users.FindByEmailAsync(email) : null;
        if (user is { EmailConfirmed: false })
        {
            await mailer.SendConfirmationAsync(user, ct);
        }

        return TypedResults.Accepted((string?)null);
    }

    private static async Task<Accepted> ForgotPassword(EmailRequest request, UserManager<AppUser> users, AccountMailer mailer, CancellationToken ct)
    {
        var user = AccountInput.TryNormalizeEmail(request.Email, out var email) ? await users.FindByEmailAsync(email) : null;
        if (user is not null)
        {
            await mailer.SendPasswordResetAsync(user, ct);
        }

        return TypedResults.Accepted((string?)null);
    }

    private static async Task<Results<NoContent, ValidationProblem, ProblemHttpResult>> ResetPassword(ResetPasswordRequest request, UserManager<AppUser> users)
    {
        var user = await users.FindByIdAsync(request.UserId.ToString());
        var token = AccountInput.DecodeToken(request.Code);
        if (user is null || token is null)
        {
            return InvalidLink();
        }

        var passwordCheck = await AccountInput.ValidatePasswordAsync(users, user, request.NewPassword);
        if (!passwordCheck.Succeeded)
        {
            return Problems.FromIdentity(passwordCheck, "newPassword");
        }

        var result = await users.ResetPasswordAsync(user, token, request.NewPassword);
        if (!result.Succeeded)
        {
            return result.Errors.Any(e => e.Code == nameof(IdentityErrorDescriber.InvalidToken))
                ? InvalidLink()
                : Problems.FromIdentity(result, "newPassword");
        }

        // The reset link went to this address, so it is proven.
        if (!user.EmailConfirmed)
        {
            user.EmailConfirmed = true;
        }

        await users.ResetAccessFailedCountAsync(user);
        await users.SetLockoutEndDateAsync(user, null);
        return TypedResults.NoContent();
    }

    private static string? Item(IDictionary<string, string?> items, string key) =>
        items.TryGetValue(key, out var value) ? value : null;

    private static ProblemHttpResult InvalidLink() =>
        Problems.Coded(StatusCodes.Status400BadRequest, "invalid_link", "This link is invalid or has expired.");
}
