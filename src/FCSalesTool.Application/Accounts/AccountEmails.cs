using System.Net;
using FCSalesTool.Application.Email;

namespace FCSalesTool.Application.Accounts;

/// <summary>Builds the account emails. Links are passed in fully formed (absolute URLs).</summary>
public static class AccountEmails
{
    public static EmailMessage ConfirmEmail(string siteName, string to, Uri link) => Build(
        siteName,
        to,
        subject: $"Confirm your email for {siteName}",
        intro: $"Confirm this email address to finish setting up your {siteName} account.",
        action: "Confirm email",
        link,
        outro: "If you didn't create an account, you can ignore this email.");

    public static EmailMessage ConfirmEmailChange(string siteName, string to, Uri link) => Build(
        siteName,
        to,
        subject: $"Confirm your new email for {siteName}",
        intro: $"Confirm this address to use it for your {siteName} account.",
        action: "Confirm new email",
        link,
        outro: "If you didn't ask for this, you can ignore this email. Your account is unchanged.");

    public static EmailMessage ResetPassword(string siteName, string to, Uri link) => Build(
        siteName,
        to,
        subject: $"Reset your {siteName} password",
        intro: "Someone asked to reset the password for this account. Use the link below to choose a new one.",
        action: "Reset password",
        link,
        outro: "If it wasn't you, ignore this email. Your password stays the same.");

    /// <summary>Sent instead of a confirmation when someone registers with an address that already has an account.</summary>
    public static EmailMessage AlreadyRegistered(string siteName, string to, Uri signInLink, Uri resetLink) => new(
        to,
        $"You already have a {siteName} account",
        $"""
        Someone tried to create a {siteName} account with this email address, but you already have one.

        Sign in: {signInLink}
        Forgot your password? Reset it: {resetLink}

        If it wasn't you, you can ignore this email.
        """,
        Html(
            siteName,
            $"<p>Someone tried to create a {Enc(siteName)} account with this email address, but you already have one.</p>"
            + $"<p><a href=\"{Enc(signInLink.AbsoluteUri)}\">Sign in</a> · <a href=\"{Enc(resetLink.AbsoluteUri)}\">Reset your password</a></p>"
            + "<p>If it wasn't you, you can ignore this email.</p>"));

    private static EmailMessage Build(string siteName, string to, string subject, string intro, string action, Uri link, string outro) => new(
        to,
        subject,
        $"""
        {intro}

        {action}: {link.AbsoluteUri}

        {outro}
        """,
        Html(
            siteName,
            $"<p>{Enc(intro)}</p>"
            + $"<p><a href=\"{Enc(link.AbsoluteUri)}\">{Enc(action)}</a></p>"
            + $"<p>Or paste this link into your browser:<br>{Enc(link.AbsoluteUri)}</p>"
            + $"<p>{Enc(outro)}</p>"));

    private static string Html(string siteName, string body) =>
        $"<!doctype html><html><body style=\"font-family:sans-serif;line-height:1.5\">{body}<p>— {Enc(siteName)}</p></body></html>";

    private static string Enc(string value) => WebUtility.HtmlEncode(value);
}
