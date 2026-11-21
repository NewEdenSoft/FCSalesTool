using FCSalesTool.Application.Email;
using MailKit;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

namespace FCSalesTool.Infrastructure.Email;

public sealed class EmailOptions
{
    public const string Section = "Email";

    public string FromAddress { get; set; } = "no-reply@omegawatch.local";

    public string FromName { get; set; } = "OmegaWatch";

    /// <summary>SMTP server. In local dev the AppHost points this at a Mailpit container.</summary>
    public SmtpOptions Smtp { get; set; } = new();
}

public sealed class SmtpOptions
{
    public string? Host { get; set; }

    public int Port { get; set; } = 587;

    public string? Username { get; set; }

    public string? Password { get; set; }

    /// <summary>MailKit <see cref="SecureSocketOptions"/>: Auto, None, StartTls, SslOnConnect.</summary>
    public SecureSocketOptions Security { get; set; } = SecureSocketOptions.Auto;
}

internal sealed class SmtpEmailSender(IOptions<EmailOptions> options) : IEmailSender
{
    public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        var settings = options.Value;
        var mime = new MimeMessage();
        var defaults = new EmailOptions();
        mime.From.Add(new MailboxAddress(
            string.IsNullOrWhiteSpace(settings.FromName) ? defaults.FromName : settings.FromName,
            string.IsNullOrWhiteSpace(settings.FromAddress) ? defaults.FromAddress : settings.FromAddress));
        mime.To.Add(MailboxAddress.Parse(message.To));
        mime.Subject = message.Subject;
        mime.Body = new BodyBuilder { TextBody = message.TextBody, HtmlBody = message.HtmlBody }.ToMessageBody();

        using var client = new SmtpClient();
        await client.ConnectAsync(settings.Smtp.Host ?? throw new InvalidOperationException("Email:Smtp:Host is not set."), settings.Smtp.Port, settings.Smtp.Security, cancellationToken);
        if (!string.IsNullOrEmpty(settings.Smtp.Username))
        {
            await client.AuthenticateAsync(settings.Smtp.Username, settings.Smtp.Password ?? string.Empty, cancellationToken);
        }

        await client.SendAsync(FormatOptions.Default, new SendRequest(mime), cancellationToken);
        await client.DisconnectAsync(true, cancellationToken);
    }
}

/// <summary>Development fallback when no SMTP server is configured: writes the email to the log.</summary>
internal sealed partial class LoggingEmailSender(ILogger<LoggingEmailSender> logger) : IEmailSender
{
    public Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        LogEmail(message.To, message.Subject, message.TextBody);
        return Task.CompletedTask;
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "No SMTP configured. Email to {To}: {Subject}\n{Body}")]
    private partial void LogEmail(string to, string subject, string body);
}
