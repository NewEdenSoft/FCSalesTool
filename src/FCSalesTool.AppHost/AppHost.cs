var builder = DistributedApplication.CreateBuilder(args);

// PostgreSQL server + application database. Persistent so data survives AppHost restarts.
var postgres = builder.AddPostgres("postgres")
    .WithDataVolume()
    .WithLifetime(ContainerLifetime.Persistent);

var db = postgres.AddDatabase("fcsalestool");

// ASP.NET Core API (EVE SSO, minimal API endpoints). Receives ConnectionStrings__fcsalestool.
#pragma warning disable ASPIREDOTNETPROJECT001 // AddDotnetProject is experimental in Aspire 17
var api = builder.AddDotnetProject("api", "../FCSalesTool.Api/FCSalesTool.Api.csproj")
#pragma warning restore ASPIREDOTNETPROJECT001
    .WithReference(db)
    .WaitFor(db)
    .WithHttpHealthCheck("/health");

// React SPA (Vite dev server). Proxies /api and /auth to the API using the
// injected services__api__* / API_HTTP(S) environment variables (see web/vite.config.ts).
builder.AddViteApp("web", "../../web")
    .WithPnpm()
    .WithReference(api)
    .WaitFor(api)
    .WithExternalHttpEndpoints();

builder.Build().Run();
