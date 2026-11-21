namespace FCSalesTool.Application.Email;

public sealed record EmailMessage(string To, string Subject, string TextBody, string HtmlBody);

/// <summary>Sends transactional email (account confirmation, password reset).</summary>
public interface IEmailSender
{
    Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default);
}
