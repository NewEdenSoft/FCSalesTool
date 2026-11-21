using System.Net;
using System.Net.Http.Json;
using FCSalesTool.Api.Account;
using FCSalesTool.Api.Auth;
using FCSalesTool.Api.Common;
using FCSalesTool.Infrastructure.Identity;

namespace FCSalesTool.Api.Tests;

public sealed class EveSignInTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private static CancellationToken Ct => TestApi.Ct;
    private static long _nextCharacterId = 95000000;

    private static long NewCharacterId() => Interlocked.Increment(ref _nextCharacterId);

    [Fact]
    public async Task First_sign_in_creates_an_account_named_after_the_character()
    {
        using var client = factory.CreateBrowserClient();
        var id = NewCharacterId();

        var response = await client.CompleteAsync(factory, new EveIdentity(id, "Amarr Pilot", "owner-1"), returnUrl: "/planner");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/planner", response.Headers.Location?.OriginalString);
        var me = await client.GetFromJsonAsync<MeResponse>("/api/me", Ct);
        Assert.Equal("Amarr Pilot", me!.DisplayName);
        Assert.Equal(id, me.AvatarCharacterId);
        Assert.Null(me.Email);
        Assert.False(me.HasPassword);
        Assert.Equal(id, Assert.Single(me.Characters).CharacterId);
    }

    [Fact]
    public async Task Signing_in_again_finds_the_same_account()
    {
        var id = NewCharacterId();
        using var first = factory.CreateBrowserClient();
        await first.CompleteAsync(factory, new EveIdentity(id, "Caldari Pilot", "owner-1"));
        var firstMe = await first.GetFromJsonAsync<MeResponse>("/api/me", Ct);
        await first.PatchAsJsonAsync("/api/me", new { displayName = "My own name" }, Ct);

        using var second = factory.CreateBrowserClient();
        await second.CompleteAsync(factory, new EveIdentity(id, "Caldari Pilot", "owner-1"));
        var secondMe = await second.GetFromJsonAsync<MeResponse>("/api/me", Ct);

        Assert.Equal(firstMe!.Id, secondMe!.Id);
        Assert.Equal("My own name", secondMe.DisplayName); // A custom name isn't overwritten.
    }

    [Fact]
    public async Task Transferred_character_gets_a_new_account_and_leaves_the_old_one()
    {
        var id = NewCharacterId();
        using var seller = factory.CreateBrowserClient();
        await seller.CompleteAsync(factory, new EveIdentity(id, "Sold Pilot", "owner-seller"));
        var sellerMe = await seller.GetFromJsonAsync<MeResponse>("/api/me", Ct);

        using var buyer = factory.CreateBrowserClient();
        await buyer.CompleteAsync(factory, new EveIdentity(id, "Sold Pilot", "owner-buyer"));
        var buyerMe = await buyer.GetFromJsonAsync<MeResponse>("/api/me", Ct);

        Assert.NotEqual(sellerMe!.Id, buyerMe!.Id);
        Assert.Equal(id, Assert.Single(buyerMe.Characters).CharacterId);
    }

    [Fact]
    public async Task Email_account_links_a_character_which_can_then_sign_in()
    {
        using var client = factory.CreateBrowserClient();
        var me = await client.SignUpAndSignIn(factory, TestApi.NewEmail());
        var id = NewCharacterId();

        var link = await client.CompleteAsync(factory, new EveIdentity(id, "Linked Pilot", "owner-l"), intent: "link", returnUrl: "/account", startedByUserId: me.Id.ToString());

        Assert.Equal($"/account?linked={id}", link.Headers.Location?.OriginalString);
        me = (await client.GetFromJsonAsync<MeResponse>("/api/me", Ct))!;
        Assert.Equal(id, me.AvatarCharacterId);

        using var other = factory.CreateBrowserClient();
        await other.CompleteAsync(factory, new EveIdentity(id, "Linked Pilot", "owner-l"));
        Assert.Equal(me.Id, (await other.GetFromJsonAsync<MeResponse>("/api/me", Ct))!.Id);
    }

    [Fact]
    public async Task Linking_a_character_that_belongs_to_another_account_is_refused()
    {
        var id = NewCharacterId();
        using var owner = factory.CreateBrowserClient();
        await owner.CompleteAsync(factory, new EveIdentity(id, "Taken Pilot", "owner-t"));

        using var client = factory.CreateBrowserClient();
        var me = await client.SignUpAndSignIn(factory, TestApi.NewEmail());
        var link = await client.CompleteAsync(factory, new EveIdentity(id, "Taken Pilot", "owner-t"), intent: "link", returnUrl: "/account", startedByUserId: me.Id.ToString());

        Assert.Equal("/account?error=eve_linked_elsewhere", link.Headers.Location?.OriginalString);
        Assert.Empty((await client.GetFromJsonAsync<MeResponse>("/api/me", Ct))!.Characters);
    }

    [Fact]
    public async Task Link_completed_in_a_different_session_is_refused()
    {
        using var client = factory.CreateBrowserClient();
        await client.SignUpAndSignIn(factory, TestApi.NewEmail());

        var link = await client.CompleteAsync(factory, new EveIdentity(NewCharacterId(), "Pilot", "o"), intent: "link", startedByUserId: Guid.NewGuid().ToString());

        Assert.Equal("/login?error=eve_link_session", link.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task Complete_without_an_sso_result_goes_back_to_login()
    {
        using var client = factory.CreateBrowserClient();

        var response = await client.GetAsync("/auth/eve/complete", Ct);

        Assert.Equal("/login?error=eve_failed", response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task External_return_urls_are_ignored()
    {
        using var client = factory.CreateBrowserClient();

        var response = await client.CompleteAsync(factory, new EveIdentity(NewCharacterId(), "Pilot", "o"), returnUrl: "//evil.example/steal");

        Assert.Equal("/", response.Headers.Location?.OriginalString);
    }

    [Theory]
    [InlineData("/account", "/account")]
    [InlineData("/", "/")]
    [InlineData("//evil.example", null)]
    [InlineData("/\\evil.example", null)]
    [InlineData("https://evil.example", null)]
    [InlineData("account", null)]
    [InlineData("", null)]
    public void LocalUrl_only_allows_same_site_paths(string input, string? expected) =>
        Assert.Equal(expected, LocalUrl.Sanitize(input));

    [Fact]
    public void Empty_scope_is_removed_from_the_authorization_url()
    {
        var url = EveTokenValidator.RemoveEmptyScope("https://login.eveonline.com/v2/oauth/authorize?client_id=abc&scope=&response_type=code&state=xyz");

        Assert.DoesNotContain("scope", url);
        Assert.Contains("client_id=abc", url);
        Assert.Contains("state=xyz", url);
    }

    [Fact]
    public void Requested_scopes_are_kept()
    {
        const string url = "https://login.eveonline.com/v2/oauth/authorize?client_id=abc&scope=publicData";

        Assert.Equal(url, EveTokenValidator.RemoveEmptyScope(url));
    }
}
