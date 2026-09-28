using Application.Dtos;
using Application.ViewModels.Authentication;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace WebApi.IntegrationTests.Extensions;

public static class HttpClientExtensions
{
    public static async Task AuthenticateAsync(
        this HttpClient client,
        string email = "integration-admin@example.com",
        string password = "TestAdmin123!")
    {
        var request = new SignInViewModel
        {
            Email = email,
            Password = password
        };

        var response = await client.PostAsJsonAsync(
            "/api/v1/authenticate/sign-in",
            request);

        response.EnsureSuccessStatusCode();

        var signInResponse =
            await response.Content.ReadFromJsonAsync<SignInResponseDto>();

        if (signInResponse is null ||
            string.IsNullOrWhiteSpace(signInResponse.AccessToken))
        {
            throw new InvalidOperationException(
                "Authentication response does not contain an access token.");
        }

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                signInResponse.AccessToken);
    }
}