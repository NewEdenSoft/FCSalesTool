using FCSalesTool.Application.Email;
using FCSalesTool.Infrastructure.Email;
using FCSalesTool.Infrastructure.Identity;
using FCSalesTool.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace FCSalesTool.Infrastructure;

public static class DependencyInjection
{
    public const string ConnectionStringName = "fcsalestool";

    public static IHostApplicationBuilder AddInfrastructure(this IHostApplicationBuilder builder)
    {
        var connectionString = builder.Configuration.GetConnectionString(ConnectionStringName);
        builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(
            connectionString ?? throw new InvalidOperationException(
                $"Connection string '{ConnectionStringName}' is missing. Run the app through the Aspire AppHost.")));

        builder.Services.AddSingleton(TimeProvider.System);
        builder.Services.AddScoped<EveAccountService>();

        builder.Services.Configure<EmailOptions>(builder.Configuration.GetSection(EmailOptions.Section));
        var smtpHost = builder.Configuration[$"{EmailOptions.Section}:Smtp:Host"];
        if (!string.IsNullOrEmpty(smtpHost))
        {
            builder.Services.AddSingleton<IEmailSender, SmtpEmailSender>();
        }
        else if (builder.Environment.IsDevelopment())
        {
            builder.Services.AddSingleton<IEmailSender, LoggingEmailSender>();
        }
        else
        {
            throw new InvalidOperationException("Email:Smtp:Host must be configured outside Development.");
        }

        return builder;
    }
}
