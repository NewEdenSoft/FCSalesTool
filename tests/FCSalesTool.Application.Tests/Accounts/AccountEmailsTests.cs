using FCSalesTool.Application.Accounts;

namespace FCSalesTool.Application.Tests.Accounts;

public sealed class AccountEmailsTests
{
    private static readonly Uri Link = new("https://omegawatch.example/confirm-email?userId=1&code=a<b");

    [Fact]
    public void Confirmation_email_contains_the_link_in_both_bodies()
    {
        var message = AccountEmails.ConfirmEmail("OmegaWatch", "pilot@example.com", Link);

        Assert.Equal("pilot@example.com", message.To);
        Assert.Contains("OmegaWatch", message.Subject);
        Assert.Contains(Link.AbsoluteUri, message.TextBody);
        Assert.Contains("https://omegawatch.example/confirm-email", message.HtmlBody);
    }

    [Fact]
    public void Html_body_is_encoded()
    {
        var message = AccountEmails.ResetPassword("<Omega>", "pilot@example.com", Link);

        Assert.DoesNotContain("<Omega>", message.HtmlBody);
        Assert.Contains("&lt;Omega&gt;", message.HtmlBody);
    }

    [Fact]
    public void Already_registered_email_links_to_sign_in_and_reset()
    {
        var message = AccountEmails.AlreadyRegistered(
            "OmegaWatch", "pilot@example.com", new Uri("https://o.example/login"), new Uri("https://o.example/forgot-password"));

        Assert.Contains("https://o.example/login", message.TextBody);
        Assert.Contains("https://o.example/forgot-password", message.TextBody);
    }
}
