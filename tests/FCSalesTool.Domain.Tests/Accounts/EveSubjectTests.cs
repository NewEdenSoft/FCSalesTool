using FCSalesTool.Domain.Accounts;

namespace FCSalesTool.Domain.Tests.Accounts;

public sealed class EveSubjectTests
{
    [Fact]
    public void Parses_the_character_id()
    {
        Assert.True(EveSubject.TryParseCharacterId("CHARACTER:EVE:2112625428", out var id));
        Assert.Equal(2112625428, id);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("CHARACTER:EVE:")]
    [InlineData("CHARACTER:EVE:abc")]
    [InlineData("CHARACTER:EVE:-5")]
    [InlineData("CHARACTER:EVE:0")]
    [InlineData("CORPORATION:EVE:123")]
    [InlineData("character:eve:123")]
    public void Rejects_anything_else(string? subject)
    {
        Assert.False(EveSubject.TryParseCharacterId(subject, out _));
    }
}
