using FCSalesTool.Domain.Accounts;

namespace FCSalesTool.Domain.Tests.Accounts;

public sealed class DisplayNameTests
{
    [Theory]
    [InlineData("Jita Trader", "Jita Trader")]
    [InlineData("  padded  ", "padded")]
    [InlineData("many    inner \t spaces", "many inner spaces")]
    [InlineData("Ünïcödé Pilot", "Ünïcödé Pilot")]
    public void Valid_names_are_normalized(string input, string expected)
    {
        Assert.True(DisplayName.TryNormalize(input, out var name, out var error));
        Assert.Equal(expected, name);
        Assert.Null(error);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("x")]
    [InlineData("bad\u0000name")]
    [InlineData("zero​width")]
    public void Invalid_names_are_rejected(string? input)
    {
        Assert.False(DisplayName.TryNormalize(input, out _, out var error));
        Assert.NotNull(error);
    }

    [Fact]
    public void Names_longer_than_the_maximum_are_rejected()
    {
        Assert.True(DisplayName.TryNormalize(new string('a', DisplayName.MaxLength), out _, out _));
        Assert.False(DisplayName.TryNormalize(new string('a', DisplayName.MaxLength + 1), out _, out _));
    }

    [Fact]
    public void Default_from_email_is_the_address()
    {
        Assert.Equal("pilot@example.com", DisplayName.FromEmail(" pilot@example.com "));
    }

    [Fact]
    public void Default_from_a_very_long_email_fits()
    {
        var email = new string('p', 100) + "@example.com";

        var name = DisplayName.FromEmail(email);

        Assert.Equal(DisplayName.MaxLength, name.Length);
        Assert.True(DisplayName.TryNormalize(name, out _, out _));
    }
}
