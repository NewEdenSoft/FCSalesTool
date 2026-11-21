using System.Globalization;

namespace FCSalesTool.Domain.Accounts;

/// <summary>
/// The <c>sub</c> claim of an EVE SSO token has the form <c>CHARACTER:EVE:&lt;characterId&gt;</c>.
/// </summary>
public static class EveSubject
{
    private const string Prefix = "CHARACTER:EVE:";

    public static bool TryParseCharacterId(string? subject, out long characterId)
    {
        characterId = 0;
        if (subject is null || !subject.StartsWith(Prefix, StringComparison.Ordinal))
        {
            return false;
        }

        return long.TryParse(subject.AsSpan(Prefix.Length), NumberStyles.None, CultureInfo.InvariantCulture, out characterId)
            && characterId > 0;
    }
}
