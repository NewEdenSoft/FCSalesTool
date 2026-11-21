using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using FCSalesTool.Application.Email;
using FCSalesTool.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace FCSalesTool.Api.Tests;

/// <summary>The API with an in-memory SQLite database and a capturing email sender.</summary>
public sealed class ApiFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    public CapturingEmailSender Email { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        _connection.Open();
        builder.UseEnvironment("Development");
        builder.UseSetting("ConnectionStrings:fcsalestool", "Host=unused");
        builder.UseSetting("Database:MigrateOnStartup", "false");
        builder.UseSetting("Auth:RateLimitPerMinute", "100000");
        builder.UseSetting("Auth:Eve:ClientId", "");
        builder.UseSetting("Auth:Eve:ClientSecret", "");

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IDbContextOptionsConfiguration<AppDbContext>>();
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.AddDbContext<AppDbContext>(o => o.UseSqlite(_connection));

            services.RemoveAll<IEmailSender>();
            services.AddSingleton<IEmailSender>(Email);
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);
        using var scope = host.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.EnsureCreated();
        return host;
    }

    /// <summary>A client that keeps cookies and sends the CSRF header like the SPA does.</summary>
    public HttpClient CreateBrowserClient()
    {
        var client = CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, HandleCookies = true });
        client.DefaultRequestHeaders.Add("X-Requested-With", "fetch");
        return client;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            _connection.Dispose();
        }
    }
}

public sealed partial class CapturingEmailSender : IEmailSender
{
    private readonly ConcurrentQueue<EmailMessage> _messages = new();

    public IReadOnlyList<EmailMessage> To(string address) =>
        _messages.Where(m => string.Equals(m.To, address, StringComparison.OrdinalIgnoreCase)).ToList();

    public Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        _messages.Enqueue(message);
        return Task.CompletedTask;
    }

    /// <summary>Query parameters of the first link in the latest email to <paramref name="address"/>.</summary>
    public (Uri Link, Dictionary<string, string> Query) LatestLink(string address)
    {
        var message = To(address)[^1];
        var link = new Uri(LinkPattern().Match(message.TextBody).Value);
        var query = QueryHelpers.ParseQuery(link.Query).ToDictionary(p => p.Key, p => p.Value.ToString());
        return (link, query);
    }

    [GeneratedRegex(@"https?://\S+")]
    private static partial Regex LinkPattern();
}
