using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;

namespace FCSalesTool.Api.Common;

public sealed class AppOptions
{
    public const string Section = "App";

    public string SiteName { get; set; } = "OmegaWatch";

    /// <summary>
    /// Public origin of the site (e.g. https://omegawatch.example). Used for links in emails.
    /// Required outside Development so a forged Host header can't redirect reset links.
    /// </summary>
    public string? PublicBaseUrl { get; set; }
}

/// <summary>Builds absolute links to SPA pages, for emails.</summary>
public sealed class AppUrls(IOptions<AppOptions> options, IHostEnvironment environment, IHttpContextAccessor accessor)
{
    public Uri Page(string path, IDictionary<string, string?>? query = null)
    {
        var relative = query is null ? path : QueryHelpers.AddQueryString(path, query);
        return new Uri(BaseUri(), relative);
    }

    private Uri BaseUri()
    {
        if (!string.IsNullOrEmpty(options.Value.PublicBaseUrl))
        {
            return new Uri(options.Value.PublicBaseUrl);
        }

        // Dev only: the Vite proxy forwards the browser's host (X-Forwarded-Host).
        var request = accessor.HttpContext?.Request;
        if (environment.IsDevelopment() && request is not null)
        {
            return new Uri($"{request.Scheme}://{request.Host}");
        }

        throw new InvalidOperationException("App:PublicBaseUrl must be configured.");
    }
}

public static class LocalUrl
{
    /// <summary>Only same-site absolute paths are allowed as return URLs (no open redirects).</summary>
    public static string? Sanitize(string? url) =>
        !string.IsNullOrEmpty(url)
        && url[0] == '/'
        && (url.Length == 1 || (url[1] != '/' && url[1] != '\\'))
        && !url.Contains('\\')
        && !url.Any(char.IsControl)
            ? url
            : null;
}
