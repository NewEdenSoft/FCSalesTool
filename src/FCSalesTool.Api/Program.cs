var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// OpenAPI document; the frontend's TypeScript client is generated from it.
builder.Services.AddOpenApi();

var app = builder.Build();

app.MapDefaultEndpoints();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

var api = app.MapGroup("/api");

api.MapGet("/status", () => TypedResults.Ok(new StatusResponse("ok")))
    .WithName("GetStatus");

app.Run();

public sealed record StatusResponse(string Status);
