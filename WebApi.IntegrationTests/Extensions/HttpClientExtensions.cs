using System.Net.Http.Headers;
using System.Net.Http.Json;
using Application.Dtos;
using Application.Entities;
using Application.ViewModels;
using Application.ViewModels.Authentication;

namespace WebApi.IntegrationTests.Extensions;

public static class HttpClientExtensions
{
    public static async Task AuthenticateAsync(
        this HttpClient client,
        string? role = null,
        string email = "integration-admin@example.com",
        string password = "TestAdmin123!")
    {
        // 1. Sign in
        var signInRequest = new SignInViewModel
        {
            Email = email,
            Password = password
        };

        var signInResponse = await client.PostAsJsonAsync(
            "/api/v1/authenticate/sign-in",
            signInRequest);

        signInResponse.EnsureSuccessStatusCode();

        var signInResult =
            await signInResponse.Content
                .ReadFromJsonAsync<SignInResponseDto>();

        if (signInResult is null ||
            string.IsNullOrWhiteSpace(signInResult.AccessToken))
        {
            throw new InvalidOperationException(
                "Sign-in response does not contain an access token.");
        }

        SetBearerToken(client, signInResult.AccessToken);

        // User doesn't need to select an active role.
        if (string.IsNullOrWhiteSpace(role))
            return;

        // 2. Select active role
        var selectRoleRequest = new SelectRoleViewModel
        {
            RoleCode = role
        };

        var selectRoleResponse = await client.PostAsJsonAsync(
            "/api/v1/me/select-role",
            selectRoleRequest);

        selectRoleResponse.EnsureSuccessStatusCode();

        var tokenResult =
            await selectRoleResponse.Content
                .ReadFromJsonAsync<TokenResponse>();

        if (tokenResult is null ||
            string.IsNullOrWhiteSpace(tokenResult.Token))
        {
            throw new InvalidOperationException(
                "Select-role response does not contain an access token.");
        }

        // 3. Replace the original token with role-specific token
        SetBearerToken(client, tokenResult.Token);
    }

    private static void SetBearerToken(
        HttpClient client,
        string token)
    {
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                token);
    }
}