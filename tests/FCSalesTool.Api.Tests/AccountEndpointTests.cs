using System.Net;
using System.Net.Http.Json;
using FCSalesTool.Api.Account;
using FCSalesTool.Infrastructure.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace FCSalesTool.Api.Tests;

public sealed class AccountEndpointTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private static CancellationToken Ct => TestApi.Ct;

    [Fact]
    public async Task Display_name_can_be_changed_and_is_validated()
    {
        using var client = factory.CreateBrowserClient();
        await client.SignUpAndSignIn(factory, TestApi.NewEmail());

        var ok = await client.PatchAsJsonAsync("/api/me", new { displayName = "Capsuleer Prime" }, Ct);
        var tooShort = await client.PatchAsJsonAsync("/api/me", new { displayName = "x" }, Ct);

        Assert.Equal("Capsuleer Prime", (await ok.Content.ReadFromJsonAsync<MeResponse>(Ct))!.DisplayName);
        Assert.Equal(HttpStatusCode.BadRequest, tooShort.StatusCode);
        Assert.NotEmpty(await tooShort.FieldErrors("displayName"));
    }

    [Fact]
    public async Task Change_password_requires_the_current_one()
    {
        using var client = factory.CreateBrowserClient();
        var email = TestApi.NewEmail();
        await client.SignUpAndSignIn(factory, email);

        var wrong = await client.Post("/api/me/password", new { currentPassword = "nope nope nope", newPassword = "another long password" });
        var right = await client.Post("/api/me/password", new { currentPassword = TestApi.Password, newPassword = "another long password" });

        Assert.Equal(HttpStatusCode.BadRequest, wrong.StatusCode);
        Assert.NotEmpty(await wrong.FieldErrors("currentPassword"));
        Assert.Equal(HttpStatusCode.NoContent, right.StatusCode);

        // Session survives the security-stamp change.
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/me", Ct)).StatusCode);
    }

    [Fact]
    public async Task Change_email_takes_effect_after_confirming_the_new_address()
    {
        using var client = factory.CreateBrowserClient();
        var oldEmail = TestApi.NewEmail();
        var newEmail = TestApi.NewEmail();
        await client.SignUpAndSignIn(factory, oldEmail);

        Assert.Equal(HttpStatusCode.Accepted, (await client.Post("/api/me/email", new { newEmail })).StatusCode);
        Assert.Equal(oldEmail, (await client.GetFromJsonAsync<MeResponse>("/api/me", Ct))!.Email);

        var (_, query) = factory.Email.LatestLink(newEmail);
        var confirm = await client.Post("/api/auth/confirm-email-change", new { userId = query["userId"], email = query["email"], code = query["code"] });

        Assert.Equal(HttpStatusCode.NoContent, confirm.StatusCode);
        var me = await client.GetFromJsonAsync<MeResponse>("/api/me", Ct);
        Assert.Equal(newEmail, me!.Email);
        Assert.True(me.EmailConfirmed);
    }

    [Fact]
    public async Task Linked_characters_avatar_and_unlinking()
    {
        using var client = factory.CreateBrowserClient();
        var me = await client.SignUpAndSignIn(factory, TestApi.NewEmail());
        await LinkAsync(me.Id, new EveIdentity(90000001, "Pilot One", "owner-a"));
        await LinkAsync(me.Id, new EveIdentity(90000002, "Pilot Two", "owner-a"));

        me = (await client.GetFromJsonAsync<MeResponse>("/api/me", Ct))!;
        Assert.Equal(2, me.Characters.Count);
        Assert.Equal(90000001, me.AvatarCharacterId); // First linked character becomes the avatar.

        var avatar = await client.PutAsJsonAsync("/api/me/avatar", new { characterId = 90000002 }, Ct);
        Assert.Equal(90000002, (await avatar.Content.ReadFromJsonAsync<MeResponse>(Ct))!.AvatarCharacterId);

        var notMine = await client.PutAsJsonAsync("/api/me/avatar", new { characterId = 12345 }, Ct);
        Assert.Equal(HttpStatusCode.BadRequest, notMine.StatusCode);

        var unlink = await client.DeleteAsync("/api/me/characters/90000002", Ct);
        me = (await unlink.Content.ReadFromJsonAsync<MeResponse>(Ct))!;
        Assert.Single(me.Characters);
        Assert.Equal(90000001, me.AvatarCharacterId); // Avatar falls back to a remaining character.
    }

    [Fact]
    public async Task Eve_only_account_can_add_a_password_without_a_current_one()
    {
        using var client = factory.CreateBrowserClient();
        await SignInAsEveCharacter(client, new EveIdentity(90000101, "Solo Pilot", "owner-s"));

        var response = await client.Post("/api/me/password", new { newPassword = "my first password" });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.True((await client.GetFromJsonAsync<MeResponse>("/api/me", Ct))!.HasPassword);
    }

    [Fact]
    public async Task Eve_only_account_cannot_unlink_its_only_character()
    {
        using var client = factory.CreateBrowserClient();
        await SignInAsEveCharacter(client, new EveIdentity(90000102, "Only Pilot", "owner-o"));

        var response = await client.DeleteAsync("/api/me/characters/90000102", Ct);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("last_sign_in_method", await response.ProblemCode());
    }

    private async Task LinkAsync(Guid userId, EveIdentity character)
    {
        using var scope = factory.Services.CreateScope();
        var outcome = await scope.ServiceProvider.GetRequiredService<EveAccountService>().LinkAsync(userId, character, Ct);
        Assert.Equal(EveLinkOutcome.Linked, outcome);
    }

    private async Task SignInAsEveCharacter(HttpClient client, EveIdentity character)
    {
        var response = await client.CompleteAsync(factory, character);
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
    }
}
