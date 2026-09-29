using Application.CORS.Commands.Authentication;
using Application.Dtos;
using Application.ViewModels.Authentication;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using WebApi.IntegrationTests.Extensions;
using WebApi.IntegrationTests.Infrastructure;

namespace WebApi.IntegrationTests.Controllers;

public class AuthenticateControllerTests
    : IntegrationTestBase
{
    public AuthenticateControllerTests(
        CustomWebApplicationFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task SignIn_WithValidCredentials_ReturnsAccessTokenAndRefreshCookie()
    {
        // Arrange
        var request = new SignInViewModel
        {
            Email = "integration-admin@example.com",
            Password = "TestAdmin123!"
        };

        // Act
        var response = await Client.PostAsJsonAsync(
            "/api/v1/authenticate/sign-in",
            request);

        // Assert
        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var result =
            await response.Content
                .ReadFromJsonAsync<SignInResponseDto>();

        Assert.NotNull(result);
        Assert.False(
            string.IsNullOrWhiteSpace(result.AccessToken));

        Assert.True(
            result.AccessTokenExpiresAt > DateTime.UtcNow);

        Assert.True(
            response.Headers.TryGetValues(
                "Set-Cookie",
                out var cookies));

        Assert.Contains(
            cookies,
            x => x.Contains("X-Refresh-Token="));
    }
    [Fact]
    public async Task SignIn_WithUnknownEmail_ReturnsNotFound()
    {
        // Arrange
        var request = new SignInViewModel
        {
            Email = "missing-user@example.com",
            Password = "ValidPassword123!"
        };

        // Act
        var response = await Client.PostAsJsonAsync(
            "/api/v1/authenticate/sign-in",
            request);

        // Assert
        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);
    }
    [Fact]
    public async Task SignIn_WithInvalidPasswordFormat_ReturnsBadRequest()
    {
        // Arrange
        var request = new SignInViewModel
        {
            Email = "integration-admin@example.com",
            Password = "123"
        };

        // Act
        var response = await Client.PostAsJsonAsync(
            "/api/v1/authenticate/sign-in",
            request);

        // Assert
        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);

        var problem =
            await response.Content
                .ReadFromJsonAsync<ProblemDetails>();

        Assert.NotNull(problem);
        Assert.Equal(
            "Validation error",
            problem.Title);
    }
    [Fact]
    public async Task SignIn_WithWrongPassword_ReturnsUnauthorized()
    {
        // Arrange
        var request = new SignInViewModel
        {
            Email = "integration-admin@example.com",

            // Format is valid, but password is not valid.
            Password = "WrongPassword123!"
        };

        // Act
        var response = await Client.PostAsJsonAsync(
            "/api/v1/authenticate/sign-in",
            request);

        // Assert
        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }
    [Fact]
    public async Task RefreshToken_WithoutCookie_ReturnsUnauthorized()
    {
        // Arrange
        var command = new RefreshTokenCommand();

        // Act
        var response = await Client.PostAsJsonAsync(
            "/api/v1/authenticate/refresh-token",
            command);

        // Assert
        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    [Fact]
    public async Task RefreshToken_WithValidCookie_ReturnsNewAccessToken()
    {
        // Arrange
        var signInResponse = await Client.PostAsJsonAsync(
            "/api/v1/authenticate/sign-in",
            new SignInViewModel
            {
                Email = "integration-admin@example.com",
                Password = "TestAdmin123!"
            });

        Assert.Equal(
            HttpStatusCode.OK,
            signInResponse.StatusCode);

        var originalToken =
            await signInResponse.Content
                .ReadFromJsonAsync<SignInResponseDto>();

        Assert.NotNull(originalToken);

        // WebApplicationFactory HttpClient normally keeps cookies.
        var command = new RefreshTokenCommand();

        // Act
        var refreshResponse =
            await Client.PostAsJsonAsync(
                "/api/v1/authenticate/refresh-token",
                command);

        // Assert
        Assert.Equal(
            HttpStatusCode.OK,
            refreshResponse.StatusCode);

        var refreshedToken =
            await refreshResponse.Content
                .ReadFromJsonAsync<SignInResponseDto>();

        Assert.NotNull(refreshedToken);

        Assert.False(
            string.IsNullOrWhiteSpace(
                refreshedToken.AccessToken));

        Assert.NotEqual(
            originalToken.AccessToken,
            refreshedToken.AccessToken);

        Assert.True(
            refreshResponse.Headers.TryGetValues(
                "Set-Cookie",
                out var cookies));

        Assert.Contains(
            cookies,
            x => x.Contains("X-Refresh-Token="));
    }

    [Fact]
    public async Task SignOut_WithoutAuthentication_ReturnsUnauthorized()
    {
        // Act
        var response =
            await Client.DeleteAsync(
                "/api/v1/authenticate/sign-out");

        // Assert
        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    [Fact]
    public async Task SignOut_WithAuthentication_ReturnsOk()
    {
        // Arrange
        await Client.AuthenticateAsync();

        // Act
        var response =
            await Client.DeleteAsync(
                "/api/v1/authenticate/sign-out");

        // Assert
        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);
    }

    [Fact]
    public async Task SignOut_ThenRefreshToken_ReturnsUnauthorized()
    {
        // Arrange
        var signInResponse = await Client.PostAsJsonAsync(
            "/api/v1/authenticate/sign-in",
            new SignInViewModel
            {
                Email = "integration-admin@example.com",
                Password = "TestAdmin123!"
            });

        Assert.Equal(
            HttpStatusCode.OK,
            signInResponse.StatusCode);

        var signInResult =
            await signInResponse.Content
                .ReadFromJsonAsync<SignInResponseDto>();

        Assert.NotNull(signInResult);

        Client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                signInResult.AccessToken);

        var signOutResponse =
            await Client.DeleteAsync(
                "/api/v1/authenticate/sign-out");

        Assert.Equal(
            HttpStatusCode.OK,
            signOutResponse.StatusCode);

        // Remove access token. We want to test refresh cookie only.
        Client.DefaultRequestHeaders.Authorization = null;

        // Act
        var refreshResponse =
            await Client.PostAsJsonAsync(
                "/api/v1/authenticate/refresh-token",
                new RefreshTokenCommand());

        // Assert
        Assert.Equal(
            HttpStatusCode.Unauthorized,
            refreshResponse.StatusCode);
    }
}