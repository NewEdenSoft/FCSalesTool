using System.ComponentModel.DataAnnotations;
using System.Text;
using FCSalesTool.Api.Common;
using FCSalesTool.Application.Accounts;
using FCSalesTool.Application.Email;
using FCSalesTool.Infrastructure.Identity;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;

namespace FCSalesTool.Api.Account;

internal static class Problems
{
    /// <summary>ProblemDetails with a machine-readable <c>code</c> the SPA switches on.</summary>
    public static ProblemHttpResult Coded(int status, string code, string title, string? detail = null) =>
        TypedResults.Problem(
            title: title,
            detail: detail,
            statusCode: status,
            extensions: new Dictionary<string, object?> { ["code"] = code });

    public static ValidationProblem Field(string field, string message) =>
        TypedResults.ValidationProblem(new Dictionary<string, string[]> { [field] = [message] });

    /// <summary>Maps Identity errors to form fields (password errors → <paramref name="passwordField"/>).</summary>
    public static ValidationProblem FromIdentity(IdentityResult result, string passwordField) =>
        TypedResults.ValidationProblem(result.Errors
            .GroupBy(e => e.Code.StartsWith("Password", StringComparison.Ordinal) ? passwordField : "form")
            .ToDictionary(g => g.Key, g => g.Select(e => e.Description).ToArray()));
}

internal static class AccountInput
{
    private static readonly EmailAddressAttribute EmailValidator = new();

    public static bool TryNormalizeEmail(string? input, out string email)
    {
        email = input?.Trim() ?? string.Empty;
        return email.Length is > 0 and <= 254 && EmailValidator.IsValid(email) && !email.Contains(' ');
    }

    /// <summary>Runs Identity's password validators without creating the user.</summary>
    public static async Task<IdentityResult> ValidatePasswordAsync(UserManager<AppUser> users, AppUser user, string? password)
    {
        if (string.IsNullOrEmpty(password))
        {
            return IdentityResult.Failed(new IdentityError { Code = "PasswordRequired", Description = "Enter a password." });
        }

        var errors = new List<IdentityError>();
        foreach (var validator in users.PasswordValidators)
        {
            var result = await validator.ValidateAsync(users, user, password);
            errors.AddRange(result.Errors);
        }

        return errors.Count == 0 ? IdentityResult.Success : IdentityResult.Failed([.. errors]);
    }

    public static string EncodeToken(string token) => WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));

    public static string? DecodeToken(string? code)
    {
        if (string.IsNullOrEmpty(code))
        {
            return null;
        }

        try
        {
            return Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(code));
        }
        catch (FormatException)
        {
            return null;
        }
    }
}

/// <summary>Generates tokens and sends the account emails with links into the SPA.</summary>
internal sealed class AccountMailer(UserManager<AppUser> users, IEmailSender email, AppUrls urls, IOptions<AppOptions> options)
{
    private string SiteName => options.Value.SiteName;

    public async Task SendConfirmationAsync(AppUser user, CancellationToken ct)
    {
        var token = await users.GenerateEmailConfirmationTokenAsync(user);
        var link = urls.Page("/confirm-email", new Dictionary<string, string?>
        {
            ["userId"] = user.Id.ToString(),
            ["code"] = AccountInput.EncodeToken(token),
        });
        await email.SendAsync(AccountEmails.ConfirmEmail(SiteName, user.Email!, link), ct);
    }

    public async Task SendEmailChangeAsync(AppUser user, string newEmail, CancellationToken ct)
    {
        var token = await users.GenerateChangeEmailTokenAsync(user, newEmail);
        var link = urls.Page("/confirm-email", new Dictionary<string, string?>
        {
            ["userId"] = user.Id.ToString(),
            ["email"] = newEmail,
            ["code"] = AccountInput.EncodeToken(token),
        });
        await email.SendAsync(AccountEmails.ConfirmEmailChange(SiteName, newEmail, link), ct);
    }

    public async Task SendPasswordResetAsync(AppUser user, CancellationToken ct)
    {
        var token = await users.GeneratePasswordResetTokenAsync(user);
        var link = urls.Page("/reset-password", new Dictionary<string, string?>
        {
            ["userId"] = user.Id.ToString(),
            ["code"] = AccountInput.EncodeToken(token),
        });
        await email.SendAsync(AccountEmails.ResetPassword(SiteName, user.Email!, link), ct);
    }

    public Task SendAlreadyRegisteredAsync(AppUser user, CancellationToken ct) => email.SendAsync(
        AccountEmails.AlreadyRegistered(SiteName, user.Email!, urls.Page("/login"), urls.Page("/forgot-password")),
        ct);
}
