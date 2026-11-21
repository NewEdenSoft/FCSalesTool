using System.Text;

namespace FCSalesTool.Domain.Accounts;

/// <summary>
/// Rules for an account's display name. It defaults to the EVE character name or the
/// email address and can be changed by the user.
/// </summary>
public static class DisplayName
{
    public const int MinLength = 2;
    public const int MaxLength = 64;

    /// <summary>
    /// Trims, collapses inner whitespace and checks length and characters.
    /// Returns an error message instead of the name when the input is invalid.
    /// </summary>
    public static bool TryNormalize(string? input, out string name, out string? error)
    {
        name = string.Empty;

        if (string.IsNullOrWhiteSpace(input))
        {
            error = "Enter a display name.";
            return false;
        }

        var builder = new StringBuilder(input.Length);
        var pendingSpace = false;
        foreach (var c in input.Trim())
        {
            if (char.IsWhiteSpace(c))
            {
                pendingSpace = true;
                continue;
            }

            if (char.IsControl(c) || char.GetUnicodeCategory(c) == System.Globalization.UnicodeCategory.Format)
            {
                error = "The display name contains characters that aren't allowed.";
                return false;
            }

            if (pendingSpace)
            {
                builder.Append(' ');
                pendingSpace = false;
            }

            builder.Append(c);
        }

        var normalized = builder.ToString();
        if (normalized.Length < MinLength || normalized.Length > MaxLength)
        {
            error = $"Use {MinLength}–{MaxLength} characters.";
            return false;
        }

        name = normalized;
        error = null;
        return true;
    }

    /// <summary>Default display name for an account registered with an email address.</summary>
    public static string FromEmail(string email)
    {
        var trimmed = email.Trim();
        if (trimmed.Length <= MaxLength)
        {
            return trimmed;
        }

        // Very long addresses: fall back to the local part, cut to fit.
        var at = trimmed.IndexOf('@');
        var local = at > 0 ? trimmed[..at] : trimmed;
        return local.Length <= MaxLength ? local : local[..MaxLength];
    }
}
