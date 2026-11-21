using Microsoft.AspNetCore.Identity;

namespace FCSalesTool.Infrastructure.Identity;

/// <summary>
/// A local OmegaWatch account. Users sign in with a confirmed email + password and/or
/// any EVE character linked to the account. EVE tokens are never stored.
/// </summary>
public sealed class AppUser : IdentityUser<Guid>
{
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>Linked character whose portrait is the account avatar.</summary>
    public long? AvatarCharacterId { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public List<LinkedCharacter> Characters { get; set; } = [];
}

/// <summary>An EVE character that can sign in to an account. One character belongs to at most one account.</summary>
public sealed class LinkedCharacter
{
    public long CharacterId { get; set; }

    public Guid UserId { get; set; }

    public AppUser? User { get; set; }

    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// EVE SSO <c>owner</c> claim. It changes when the character is transferred to another
    /// EVE account, so the new owner can't sign in to the old owner's OmegaWatch account.
    /// </summary>
    public string? OwnerHash { get; set; }

    public DateTimeOffset LinkedAt { get; set; }

    public DateTimeOffset? LastSignInAt { get; set; }
}
