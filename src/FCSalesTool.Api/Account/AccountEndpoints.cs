using System.Security.Claims;
using FCSalesTool.Domain.Accounts;
using FCSalesTool.Infrastructure.Identity;
using FCSalesTool.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;

namespace FCSalesTool.Api.Account;

/// <summary>The signed-in user's own account: profile, avatar, linked characters, email and password.</summary>
public static class AccountEndpoints
{
    public static void MapAccountEndpoints(this RouteGroupBuilder api)
    {
        var me = api.MapGroup("/me").WithTags("Account").RequireAuthorization();
        me.MapGet("/", GetMe).WithName("GetMe");
        me.MapPatch("/", UpdateProfile);
        me.MapPut("/avatar", SetAvatar);
        me.MapDelete("/characters/{characterId:long}", UnlinkCharacter);
        me.MapPost("/password", SetPassword);
        me.MapPost("/email", ChangeEmail);
    }

    private static async Task<Results<Ok<MeResponse>, UnauthorizedHttpResult>> GetMe(
        ClaimsPrincipal principal, UserManager<AppUser> users, AppDbContext db, CancellationToken ct)
    {
        var user = await users.GetUserAsync(principal);
        return user is null ? TypedResults.Unauthorized() : TypedResults.Ok(await db.ToMeAsync(user, ct));
    }

    private static async Task<Results<Ok<MeResponse>, ValidationProblem, UnauthorizedHttpResult>> UpdateProfile(
        UpdateProfileRequest request, ClaimsPrincipal principal, UserManager<AppUser> users, AppDbContext db, CancellationToken ct)
    {
        var user = await users.GetUserAsync(principal);
        if (user is null)
        {
            return TypedResults.Unauthorized();
        }

        if (!DisplayName.TryNormalize(request.DisplayName, out var name, out var error))
        {
            return Problems.Field("displayName", error!);
        }

        user.DisplayName = name;
        await users.UpdateAsync(user);
        return TypedResults.Ok(await db.ToMeAsync(user, ct));
    }

    private static async Task<Results<Ok<MeResponse>, ValidationProblem, UnauthorizedHttpResult>> SetAvatar(
        SetAvatarRequest request, ClaimsPrincipal principal, UserManager<AppUser> users, EveAccountService accounts, AppDbContext db, CancellationToken ct)
    {
        var user = await users.GetUserAsync(principal);
        if (user is null)
        {
            return TypedResults.Unauthorized();
        }

        if (!await accounts.SetAvatarAsync(user.Id, request.CharacterId, ct))
        {
            return Problems.Field("characterId", "That character isn't linked to your account.");
        }

        return TypedResults.Ok(await db.ToMeAsync(user, ct));
    }

    private static async Task<Results<Ok<MeResponse>, NotFound, ProblemHttpResult, UnauthorizedHttpResult>> UnlinkCharacter(
        long characterId, ClaimsPrincipal principal, UserManager<AppUser> users, EveAccountService accounts, AppDbContext db, CancellationToken ct)
    {
        var user = await users.GetUserAsync(principal);
        if (user is null)
        {
            return TypedResults.Unauthorized();
        }

        return await accounts.UnlinkAsync(user.Id, characterId, ct) switch
        {
            UnlinkOutcome.Unlinked => TypedResults.Ok(await db.ToMeAsync(user, ct)),
            UnlinkOutcome.LastSignInMethod => Problems.Coded(
                StatusCodes.Status409Conflict,
                "last_sign_in_method",
                "You can't unlink your only way to sign in.",
                "Add a confirmed email and a password, or link another character first."),
            _ => TypedResults.NotFound(),
        };
    }

    /// <summary>Changes the password, or adds one to an account that signs in with EVE only.</summary>
    private static async Task<Results<NoContent, ValidationProblem, UnauthorizedHttpResult>> SetPassword(
        SetPasswordRequest request, ClaimsPrincipal principal, UserManager<AppUser> users, SignInManager<AppUser> signIn)
    {
        var user = await users.GetUserAsync(principal);
        if (user is null)
        {
            return TypedResults.Unauthorized();
        }

        var check = await AccountInput.ValidatePasswordAsync(users, user, request.NewPassword);
        if (!check.Succeeded)
        {
            return Problems.FromIdentity(check, "newPassword");
        }

        IdentityResult result;
        if (user.PasswordHash is null)
        {
            result = await users.AddPasswordAsync(user, request.NewPassword);
        }
        else if (string.IsNullOrEmpty(request.CurrentPassword))
        {
            return Problems.Field("currentPassword", "Enter your current password.");
        }
        else
        {
            result = await users.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
            if (result.Errors.Any(e => e.Code == nameof(IdentityErrorDescriber.PasswordMismatch)))
            {
                return Problems.Field("currentPassword", "Your current password is incorrect.");
            }
        }

        if (!result.Succeeded)
        {
            return Problems.FromIdentity(result, "newPassword");
        }

        await signIn.RefreshSignInAsync(user);
        return TypedResults.NoContent();
    }

    /// <summary>
    /// Sends a confirmation link to the new address; the email changes only once it is confirmed.
    /// Also used to add an email to an EVE-only account.
    /// </summary>
    private static async Task<Results<Accepted, ValidationProblem, UnauthorizedHttpResult>> ChangeEmail(
        ChangeEmailRequest request, ClaimsPrincipal principal, UserManager<AppUser> users, AccountMailer mailer, CancellationToken ct)
    {
        var user = await users.GetUserAsync(principal);
        if (user is null)
        {
            return TypedResults.Unauthorized();
        }

        if (!AccountInput.TryNormalizeEmail(request.NewEmail, out var email))
        {
            return Problems.Field("newEmail", "Enter a valid email address.");
        }

        if (string.Equals(email, user.Email, StringComparison.OrdinalIgnoreCase) && user.EmailConfirmed)
        {
            return Problems.Field("newEmail", "That's already your email address.");
        }

        // Don't reveal whether another account uses the address; confirming it would fail anyway.
        var owner = await users.FindByEmailAsync(email);
        if (owner is null || owner.Id == user.Id)
        {
            await mailer.SendEmailChangeAsync(user, email, ct);
        }

        return TypedResults.Accepted((string?)null);
    }
}
