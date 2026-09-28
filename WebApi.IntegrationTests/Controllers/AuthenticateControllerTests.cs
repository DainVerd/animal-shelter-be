using Application.Dtos;
using Application.ViewModels.Authentication;
using System.Net;
using System.Net.Http.Json;
using WebApi.IntegrationTests.Infrastructure;

namespace WebApi.IntegrationTests.Controllers;

public class AuthenticateControllerTests
    : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public AuthenticateControllerTests(
        CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task SignIn_WithValidCredentials_ReturnsOk()
    {
        // Arrange
        var request = new SignInViewModel
        {
            Email = "integration-admin@example.com",
            Password = "TestAdmin123!"
        };

        // Act
        var response = await _client.PostAsJsonAsync(
            "/api/v1/authenticate/sign-in",
            request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task SignIn_WithValidCredentials_ReturnsAccessToken()
    {
        // Arrange
        var request = new SignInViewModel
        {
            Email = "integration-admin@example.com",
            Password = "TestAdmin123!"
        };

        // Act
        var response = await _client.PostAsJsonAsync(
            "/api/v1/authenticate/sign-in",
            request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result =
            await response.Content.ReadFromJsonAsync<SignInResponseDto>();

        Assert.NotNull(result);
        Assert.False(string.IsNullOrWhiteSpace(result.AccessToken));
        Assert.True(result.AccessTokenExpiresAt > DateTime.UtcNow);
    }
}