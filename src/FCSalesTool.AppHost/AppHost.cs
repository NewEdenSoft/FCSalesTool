using Microsoft.Extensions.Configuration;

var builder = DistributedApplication.CreateBuilder(args);

// Developer-local secrets (EVE SSO keys): appsettings.<Environment>.local.json is git-ignored
// and overrides the committed appsettings files and user secrets.
builder.Configuration.AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.local.json", optional: true, reloadOnChange: false);

// Fixed local ports. The EVE SSO app's callback URL must match exactly:
//   http://localhost:5173/auth/eve/callback  (browser → Vite → API, so cookies stay same-origin)
// Changing a port means updating the EVE app registration (developers.eveonline.com) and AGENTS.md.
// Locked by tests/FCSalesTool.AppHost.Tests.
const int WebPort = 5173;
const int ApiHttpPort = 5257;
const int ApiHttpsPort = 7102;
const int MailpitUiPort = 8025;

// Optional/defaulted settings come from "Parameters:<name>" (scaffolded in appsettings.Development.json,
// secrets in appsettings.Development.local.json or user secrets). A missing value resolves to "" instead of blocking startup on a prompt.
IResourceBuilder<ParameterResource> Setting(string name, bool secret = false) =>
    builder.AddParameter(name, () => builder.Configuration[$"Parameters:{name}"] ?? string.Empty, secret: secret);

var eveClientId = Setting("eve-client-id");
var eveClientSecret = Setting("eve-client-secret", secret: true);
var eveScopes = Setting("eve-scopes");
var emailFromAddress = Setting("email-from-address");
var emailFromName = Setting("email-from-name");

// PostgreSQL server + application database. Persistent so data survives AppHost restarts.
var postgres = builder.AddPostgres("postgres")
    .WithDataVolume()
    .WithLifetime(ContainerLifetime.Persistent);

var db = postgres.AddDatabase("fcsalestool");

// Mailpit catches every email the API sends (account confirmation, password reset).
// Open its "http" endpoint from the Aspire dashboard to read them.
var mailpit = builder.AddContainer("mailpit", "axllent/mailpit", "v1.34")
    .WithHttpEndpoint(port: MailpitUiPort, targetPort: 8025, name: "http")
    .WithEndpoint(targetPort: 1025, name: "smtp", scheme: "tcp");
var smtp = mailpit.GetEndpoint("smtp");

// React SPA (Vite dev server). Proxies /api and /auth to the API using the
// injected services__api__* / API_HTTP(S) environment variables (see web/vite.config.ts).
var web = builder.AddViteApp("web", "../../web")
    .WithPnpm()
    .WithEndpoint("http", endpoint => endpoint.Port = WebPort)
    .WithExternalHttpEndpoints();

// ASP.NET Core API. Every setting it needs is set here; see AGENTS.md "Configuration".
#pragma warning disable ASPIREDOTNETPROJECT001 // AddDotnetProject is experimental in Aspire 17
var api = builder.AddDotnetProject("api", "../FCSalesTool.Api/FCSalesTool.Api.csproj")
#pragma warning restore ASPIREDOTNETPROJECT001
    .WithEndpoint("http", endpoint => endpoint.Port = ApiHttpPort)
    .WithEndpoint("https", endpoint => endpoint.Port = ApiHttpsPort)
    .WithReference(db) // ConnectionStrings__fcsalestool
    .WaitFor(db)
    // Links in emails point at the SPA.
    .WithEnvironment("App__PublicBaseUrl", web.GetEndpoint("http"))
    // EVE SSO (optional): empty client id/secret disables it.
    .WithEnvironment("Auth__Eve__ClientId", eveClientId)
    .WithEnvironment("Auth__Eve__ClientSecret", eveClientSecret)
    .WithEnvironment("Auth__Eve__Scopes", eveScopes)
    // Email via Mailpit.
    .WithEnvironment("Email__FromAddress", emailFromAddress)
    .WithEnvironment("Email__FromName", emailFromName)
    .WithEnvironment("Email__Smtp__Host", smtp.Property(EndpointProperty.Host))
    .WithEnvironment("Email__Smtp__Port", smtp.Property(EndpointProperty.Port))
    .WithEnvironment("Email__Smtp__Security", "None")
    .WaitFor(mailpit)
    .WithHttpHealthCheck("/health");

web.WithReference(api).WaitFor(api);

builder.Build().Run();
