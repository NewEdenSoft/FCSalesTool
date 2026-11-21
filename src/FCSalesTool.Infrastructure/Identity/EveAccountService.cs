using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using FCSalesTool.Infrastructure.Persistence;

namespace FCSalesTool.Infrastructure.Identity;

/// <summary>What EVE SSO told us about the signed-in character (from the validated access token).</summary>
public sealed record EveIdentity(long CharacterId, string Name, string? OwnerHash);

public enum EveLinkOutcome
{
    Linked,
    AlreadyLinked,
    /// <summary>The character signs in to a different OmegaWatch account.</summary>
    LinkedToOtherAccount,
    UserNotFound,
}

public enum UnlinkOutcome
{
    Unlinked,
    NotFound,
    /// <summary>Refused: the account would have no way to sign in.</summary>
    LastSignInMethod,
}

/// <summary>
/// Maps EVE characters to local accounts. EVE SSO only proves who the character is; the
/// account, its name and settings are ours. No EVE tokens are stored.
/// </summary>
public sealed class EveAccountService(AppDbContext db, UserManager<AppUser> users, TimeProvider time)
{
    /// <summary>
    /// Finds the account the character signs in to, or creates one named after the character.
    /// A character transferred to another EVE account (different owner hash) is detached from
    /// the previous owner's account first.
    /// </summary>
    public async Task<(AppUser User, bool Created)> SignInAsync(EveIdentity identity, CancellationToken ct = default)
    {
        var now = time.GetUtcNow();
        var link = await db.LinkedCharacters.Include(c => c.User)
            .SingleOrDefaultAsync(c => c.CharacterId == identity.CharacterId, ct);

        if (link is not null && IsTransferred(link, identity))
        {
            await DetachAsync(link, ct);
            link = null;
        }

        if (link is not null)
        {
            link.Name = identity.Name;
            link.OwnerHash ??= identity.OwnerHash;
            link.LastSignInAt = now;
            await db.SaveChangesAsync(ct);
            return (link.User!, false);
        }

        var user = new AppUser
        {
            Id = Guid.CreateVersion7(),
            DisplayName = Domain.Accounts.DisplayName.TryNormalize(identity.Name, out var name, out _) ? name : $"Character {identity.CharacterId}",
            AvatarCharacterId = identity.CharacterId,
            CreatedAt = now,
        };
        user.UserName = user.Id.ToString("N");
        ThrowIfFailed(await users.CreateAsync(user));

        db.LinkedCharacters.Add(NewLink(user.Id, identity, now, signedIn: true));
        await db.SaveChangesAsync(ct);
        return (user, true);
    }

    /// <summary>Links a character to an account the user is already signed in to.</summary>
    public async Task<EveLinkOutcome> LinkAsync(Guid userId, EveIdentity identity, CancellationToken ct = default)
    {
        var user = await db.Users.SingleOrDefaultAsync(u => u.Id == userId, ct);
        if (user is null)
        {
            return EveLinkOutcome.UserNotFound;
        }

        var now = time.GetUtcNow();
        var link = await db.LinkedCharacters.SingleOrDefaultAsync(c => c.CharacterId == identity.CharacterId, ct);

        if (link is not null && IsTransferred(link, identity))
        {
            await DetachAsync(link, ct);
            link = null;
        }

        if (link is not null)
        {
            if (link.UserId != userId)
            {
                return EveLinkOutcome.LinkedToOtherAccount;
            }

            link.Name = identity.Name;
            await db.SaveChangesAsync(ct);
            return EveLinkOutcome.AlreadyLinked;
        }

        db.LinkedCharacters.Add(NewLink(userId, identity, now, signedIn: false));
        user.AvatarCharacterId ??= identity.CharacterId;
        await db.SaveChangesAsync(ct);
        return EveLinkOutcome.Linked;
    }

    public async Task<UnlinkOutcome> UnlinkAsync(Guid userId, long characterId, CancellationToken ct = default)
    {
        var user = await db.Users.Include(u => u.Characters).SingleOrDefaultAsync(u => u.Id == userId, ct);
        var link = user?.Characters.SingleOrDefault(c => c.CharacterId == characterId);
        if (user is null || link is null)
        {
            return UnlinkOutcome.NotFound;
        }

        var canUsePassword = user.PasswordHash is not null && user.EmailConfirmed;
        if (!canUsePassword && user.Characters.Count == 1)
        {
            return UnlinkOutcome.LastSignInMethod;
        }

        user.Characters.Remove(link);
        db.LinkedCharacters.Remove(link);
        if (user.AvatarCharacterId == characterId)
        {
            user.AvatarCharacterId = user.Characters.FirstOrDefault()?.CharacterId;
        }

        await db.SaveChangesAsync(ct);
        return UnlinkOutcome.Unlinked;
    }

    /// <summary>Sets the avatar to one of the account's characters, or clears it.</summary>
    public async Task<bool> SetAvatarAsync(Guid userId, long? characterId, CancellationToken ct = default)
    {
        var user = await db.Users.Include(u => u.Characters).SingleOrDefaultAsync(u => u.Id == userId, ct);
        if (user is null || (characterId is not null && user.Characters.All(c => c.CharacterId != characterId)))
        {
            return false;
        }

        user.AvatarCharacterId = characterId;
        await db.SaveChangesAsync(ct);
        return true;
    }

    private static bool IsTransferred(LinkedCharacter link, EveIdentity identity) =>
        link.OwnerHash is not null && identity.OwnerHash is not null && link.OwnerHash != identity.OwnerHash;

    private async Task DetachAsync(LinkedCharacter link, CancellationToken ct)
    {
        var previous = await db.Users.Include(u => u.Characters).SingleAsync(u => u.Id == link.UserId, ct);
        previous.Characters.Remove(link);
        db.LinkedCharacters.Remove(link);
        if (previous.AvatarCharacterId == link.CharacterId)
        {
            previous.AvatarCharacterId = previous.Characters.FirstOrDefault()?.CharacterId;
        }

        // Saved now so the new link (same key) can be inserted afterwards.
        await db.SaveChangesAsync(ct);
    }

    private static LinkedCharacter NewLink(Guid userId, EveIdentity identity, DateTimeOffset now, bool signedIn) => new()
    {
        CharacterId = identity.CharacterId,
        UserId = userId,
        Name = identity.Name,
        OwnerHash = identity.OwnerHash,
        LinkedAt = now,
        LastSignInAt = signedIn ? now : null,
    };

    private static void ThrowIfFailed(IdentityResult result)
    {
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(
                "Could not create account: " + string.Join("; ", result.Errors.Select(e => e.Description)));
        }
    }
}
