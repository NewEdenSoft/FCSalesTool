using FCSalesTool.Infrastructure.Identity;
using FCSalesTool.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FCSalesTool.Api.Account;

public sealed record RegisterRequest(string Email, string Password, string? DisplayName);

public sealed record LoginRequest(string Email, string Password, bool RememberMe);

public sealed record ConfirmEmailRequest(Guid UserId, string Code);

public sealed record ConfirmEmailChangeRequest(Guid UserId, string Email, string Code);

public sealed record EmailRequest(string Email);

public sealed record ResetPasswordRequest(Guid UserId, string Code, string NewPassword);

public sealed record UpdateProfileRequest(string DisplayName);

public sealed record SetAvatarRequest(long? CharacterId);

/// <summary><see cref="CurrentPassword"/> is required only when the account already has a password.</summary>
public sealed record SetPasswordRequest(string? CurrentPassword, string NewPassword);

public sealed record ChangeEmailRequest(string NewEmail);

public sealed record AuthProvidersResponse(bool Eve);

public sealed record CharacterResponse(long CharacterId, string Name, DateTimeOffset LinkedAt);

public sealed record MeResponse(
    Guid Id,
    string DisplayName,
    string? Email,
    bool EmailConfirmed,
    bool HasPassword,
    long? AvatarCharacterId,
    IReadOnlyList<CharacterResponse> Characters);

internal static class MeMapper
{
    public static async Task<MeResponse> ToMeAsync(this AppDbContext db, AppUser user, CancellationToken ct)
    {
        var characters = await db.LinkedCharacters
            .Where(c => c.UserId == user.Id)
            .OrderBy(c => c.Name)
            .Select(c => new CharacterResponse(c.CharacterId, c.Name, c.LinkedAt))
            .ToListAsync(ct);

        return new MeResponse(
            user.Id,
            user.DisplayName,
            user.Email,
            user.EmailConfirmed,
            user.PasswordHash is not null,
            user.AvatarCharacterId,
            characters);
    }
}
