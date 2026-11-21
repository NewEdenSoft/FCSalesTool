using FCSalesTool.Api.Account;
using FCSalesTool.Api.Auth;
using FCSalesTool.Api.Common;
using FCSalesTool.Infrastructure;
using FCSalesTool.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddInfrastructure();
builder.AddAppAuth();
builder.Services.AddScoped<AccountMailer>();
builder.Services.AddProblemDetails();

// OpenAPI document; the frontend's TypeScript client is generated from it.
builder.Services.AddOpenApi();

var app = builder.Build();

app.UseForwardedHeaders();
app.UseExceptionHandler();
app.UseStatusCodePages();

if (!app.Environment.IsDevelopment())
{
    // In dev the browser reaches the API over http through the Vite proxy.
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseCsrfHeaderCheck();
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

app.MapDefaultEndpoints();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

var api = app.MapGroup("/api");

api.MapGet("/status", () => TypedResults.Ok(new StatusResponse("ok")))
    .WithName("GetStatus");

api.MapAccountEndpoints();
app.MapAuthEndpoints();

if (app.Configuration.GetValue("Database:MigrateOnStartup", app.Environment.IsDevelopment()))
{
    await using var scope = app.Services.CreateAsyncScope();
    await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.MigrateAsync();
}

app.Run();

public sealed record StatusResponse(string Status);
