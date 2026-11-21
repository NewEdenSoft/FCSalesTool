using System.Net;
using System.Net.Http.Json;
using FCSalesTool.Api.Account;

namespace FCSalesTool.Api.Tests;

public sealed class AuthEndpointTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private static CancellationToken Ct => TestApi.Ct;

    [Fact]
    public async Task Register_confirm_login_me_logout()
    {
        using var client = factory.CreateBrowserClient();
        var email = TestApi.NewEmail();

        var me = await client.SignUpAndSignIn(factory, email);

        Assert.Equal(email, me.DisplayName); // Defaults to the email address.
        Assert.True(me.EmailConfirmed);
        Assert.True(me.HasPassword);
        Assert.Empty(me.Characters);

        var fetched = await client.GetFromJsonAsync<MeResponse>("/api/me", Ct);
        Assert.Equal(me.Id, fetched!.Id);

        Assert.Equal(HttpStatusCode.NoContent, (await client.Post("/api/auth/logout", new { })).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/me", Ct)).StatusCode);
    }

    [Fact]
    public async Task Register_uses_the_display_name_given()
    {
        using var client = factory.CreateBrowserClient();
        var email = TestApi.NewEmail();
        await client.Post("/api/auth/register", new { email, password = TestApi.Password, displayName = "  Jita   Trader " });
        var (_, query) = factory.Email.LatestLink(email);
        await client.Post("/api/auth/confirm-email", new { userId = query["userId"], code = query["code"] });

        var login = await client.Post("/api/auth/login", new { email, password = TestApi.Password });
        var me = await login.Content.ReadFromJsonAsync<MeResponse>(Ct);

        Assert.Equal("Jita Trader", me!.DisplayName);
    }

    [Fact]
    public async Task Login_before_confirming_email_is_refused()
    {
        using var client = factory.CreateBrowserClient();
        var email = TestApi.NewEmail();
        await client.Post("/api/auth/register", new { email, password = TestApi.Password });

        var login = await client.Post("/api/auth/login", new { email, password = TestApi.Password });

        Assert.Equal(HttpStatusCode.Forbidden, login.StatusCode);
        Assert.Equal("email_not_confirmed", await login.ProblemCode());
    }

    [Fact]
    public async Task Unconfirmed_account_with_wrong_password_looks_like_bad_credentials()
    {
        using var client = factory.CreateBrowserClient();
        var email = TestApi.NewEmail();
        await client.Post("/api/auth/register", new { email, password = TestApi.Password });

        var login = await client.Post("/api/auth/login", new { email, password = "not the password" });

        Assert.Equal(HttpStatusCode.Unauthorized, login.StatusCode);
        Assert.Equal("invalid_credentials", await login.ProblemCode());
    }

    [Fact]
    public async Task Wrong_password_and_unknown_email_give_the_same_answer()
    {
        using var client = factory.CreateBrowserClient();
        var email = TestApi.NewEmail();
        await client.SignUpAndSignIn(factory, email);
        await client.Post("/api/auth/logout", new { });

        var wrongPassword = await client.Post("/api/auth/login", new { email, password = "wrong password!" });
        var unknown = await client.Post("/api/auth/login", new { email = TestApi.NewEmail(), password = "wrong password!" });

        Assert.Equal(HttpStatusCode.Unauthorized, wrongPassword.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, unknown.StatusCode);
        Assert.Equal("invalid_credentials", await wrongPassword.ProblemCode());
        Assert.Equal("invalid_credentials", await unknown.ProblemCode());
    }

    [Fact]
    public async Task Account_locks_after_repeated_failures()
    {
        using var client = factory.CreateBrowserClient();
        var email = TestApi.NewEmail();
        await client.SignUpAndSignIn(factory, email);
        await client.Post("/api/auth/logout", new { });

        for (var i = 0; i < 5; i++)
        {
            await client.Post("/api/auth/login", new { email, password = "wrong password!" });
        }

        var login = await client.Post("/api/auth/login", new { email, password = TestApi.Password });
        Assert.Equal("locked_out", await login.ProblemCode());
    }

    [Fact]
    public async Task Registering_an_existing_email_does_not_reveal_it_and_notifies_the_owner()
    {
        using var client = factory.CreateBrowserClient();
        var email = TestApi.NewEmail();
        await client.SignUpAndSignIn(factory, email);

        var again = await client.Post("/api/auth/register", new { email, password = "another password" });

        Assert.Equal(HttpStatusCode.Accepted, again.StatusCode);
        Assert.Contains("already have", factory.Email.To(email)[^1].Subject);
    }

    [Fact]
    public async Task Register_rejects_short_password_and_bad_email()
    {
        using var client = factory.CreateBrowserClient();

        var shortPassword = await client.Post("/api/auth/register", new { email = TestApi.NewEmail(), password = "short" });
        var badEmail = await client.Post("/api/auth/register", new { email = "not-an-email", password = TestApi.Password });

        Assert.Equal(HttpStatusCode.BadRequest, shortPassword.StatusCode);
        Assert.NotEmpty(await shortPassword.FieldErrors("password"));
        Assert.Equal(HttpStatusCode.BadRequest, badEmail.StatusCode);
        Assert.NotEmpty(await badEmail.FieldErrors("email"));
    }

    [Fact]
    public async Task Confirm_email_with_a_tampered_code_fails()
    {
        using var client = factory.CreateBrowserClient();
        var email = TestApi.NewEmail();
        await client.Post("/api/auth/register", new { email, password = TestApi.Password });
        var (_, query) = factory.Email.LatestLink(email);

        var confirm = await client.Post("/api/auth/confirm-email", new { userId = query["userId"], code = "bm9wZQ" });

        Assert.Equal(HttpStatusCode.BadRequest, confirm.StatusCode);
        Assert.Equal("invalid_link", await confirm.ProblemCode());
    }

    [Fact]
    public async Task Resend_confirmation_sends_a_new_link()
    {
        using var client = factory.CreateBrowserClient();
        var email = TestApi.NewEmail();
        await client.Post("/api/auth/register", new { email, password = TestApi.Password });

        var resend = await client.Post("/api/auth/resend-confirmation", new { email });

        Assert.Equal(HttpStatusCode.Accepted, resend.StatusCode);
        Assert.Equal(2, factory.Email.To(email).Count);
    }

    [Fact]
    public async Task Forgot_and_reset_password()
    {
        using var client = factory.CreateBrowserClient();
        var email = TestApi.NewEmail();
        await client.SignUpAndSignIn(factory, email);
        await client.Post("/api/auth/logout", new { });

        Assert.Equal(HttpStatusCode.Accepted, (await client.Post("/api/auth/forgot-password", new { email })).StatusCode);
        var (link, query) = factory.Email.LatestLink(email);
        Assert.Equal("/reset-password", link.AbsolutePath);

        var reset = await client.Post("/api/auth/reset-password", new { userId = query["userId"], code = query["code"], newPassword = "a brand new password" });
        Assert.Equal(HttpStatusCode.NoContent, reset.StatusCode);

        var oldLogin = await client.Post("/api/auth/login", new { email, password = TestApi.Password });
        var newLogin = await client.Post("/api/auth/login", new { email, password = "a brand new password" });
        Assert.Equal(HttpStatusCode.Unauthorized, oldLogin.StatusCode);
        Assert.Equal(HttpStatusCode.OK, newLogin.StatusCode);
    }

    [Fact]
    public async Task Forgot_password_for_unknown_email_is_accepted_silently()
    {
        using var client = factory.CreateBrowserClient();
        var email = TestApi.NewEmail();

        var response = await client.Post("/api/auth/forgot-password", new { email });

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        Assert.Empty(factory.Email.To(email));
    }

    [Fact]
    public async Task Me_without_a_session_is_401_not_a_redirect()
    {
        using var client = factory.CreateBrowserClient();

        var response = await client.GetAsync("/api/me", Ct);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task State_changing_requests_need_the_csrf_header()
    {
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login", new { email = "a@b.c", password = "x" }, Ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("missing_csrf_header", await response.ProblemCode());
    }

    [Fact]
    public async Task Providers_report_eve_unavailable_when_not_configured()
    {
        using var client = factory.CreateBrowserClient();

        var providers = await client.GetFromJsonAsync<AuthProvidersResponse>("/api/auth/providers", Ct);
        var eveLogin = await client.GetAsync("/auth/eve/login", Ct);

        Assert.False(providers!.Eve);
        Assert.Equal(HttpStatusCode.Redirect, eveLogin.StatusCode);
        Assert.Equal("/login?error=eve_unavailable", eveLogin.Headers.Location?.OriginalString);
    }
}
