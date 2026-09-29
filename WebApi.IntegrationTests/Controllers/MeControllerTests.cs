using Application.Constants;
using Application.Entities;
using Application.ViewModels;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using WebApi.IntegrationTests.Extensions;
using WebApi.IntegrationTests.Infrastructure;

namespace WebApi.IntegrationTests.Controllers;

public class MeControllerTests
    : IntegrationTestBase
{
    public MeControllerTests(
        CustomWebApplicationFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task GetAvailableRoles_WithoutAuthentication_ReturnsUnauthorized()
    {
        // Act
        var response = await Client.GetAsync(
            "/api/v1/me/available-roles");

        // Assert
        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    [Fact]
    public async Task SelectRole_WithoutAuthentication_ReturnsUnauthorized()
    {
        // Arrange
        var request = new SelectRoleViewModel
        {
            RoleCode = UserRole.SuperAdmin
        };

        // Act
        var response = await Client.PostAsJsonAsync(
            "/api/v1/me/select-role",
            request);

        // Assert
        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    [Fact]
    public async Task GetProfile_WithoutAuthentication_ReturnsUnauthorized()
    {
        // Act
        var response = await Client.GetAsync(
            "/api/v1/me/profile");

        // Assert
        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }
    [Fact]
    public async Task GetAvailableRoles_WithAuthenticatedAdmin_ReturnsAssignedRoles()
    {
        // Arrange
        await Client.AuthenticateAsync();

        // Act
        var response = await Client.GetAsync(
            "/api/v1/me/available-roles");

        // Assert
        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var json = await response.Content
            .ReadFromJsonAsync<JsonElement>();

        Assert.Equal(
            JsonValueKind.Array,
            json.ValueKind);

        var roles = json
            .EnumerateArray()
            .Select(x => x.GetProperty("roleCode").GetString())
            .ToList();

        Assert.Contains(
            UserRole.SuperAdmin,
            roles);

        Assert.Contains(
            UserRole.User,
            roles);
    }

    [Fact]
    public async Task SelectRole_WithAssignedRole_ReturnsRoleSpecificToken()
    {
        // Arrange
        await Client.AuthenticateAsync();

        var request = new SelectRoleViewModel
        {
            RoleCode = UserRole.SuperAdmin
        };

        // Act
        var response = await Client.PostAsJsonAsync(
            "/api/v1/me/select-role",
            request);

        // Assert
        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var result = await response.Content
            .ReadFromJsonAsync<TokenResponse>();

        Assert.NotNull(result);

        Assert.False(
            string.IsNullOrWhiteSpace(result.Token));

        Assert.True(
            result.ExpiresAt > DateTime.UtcNow);
    }

    [Fact]
    public async Task SelectSuperAdminRole_ReturnedTokenAuthorizesRoleProtectedEndpoint()
    {
        // Arrange
        await Client.AuthenticateAsync();

        var selectRoleResponse =
            await Client.PostAsJsonAsync(
                "/api/v1/me/select-role",
                new SelectRoleViewModel
                {
                    RoleCode = UserRole.SuperAdmin
                });

        Assert.Equal(
            HttpStatusCode.OK,
            selectRoleResponse.StatusCode);

        var tokenResponse =
            await selectRoleResponse.Content
                .ReadFromJsonAsync<TokenResponse>();

        Assert.NotNull(tokenResponse);

        Client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue(
                "Bearer",
                tokenResponse.Token);

        using var content =
            new MultipartFormDataContent();

        // Act
        var response = await Client.PostAsync(
            "/api/v1/animals",
            content);

        // Assert
        // Important:
        // 400 proves authorization succeeded and request reached
        // model validation.
        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }

    [Fact]
    public async Task GetProfile_WithAuthenticatedUser_ReturnsCurrentUser()
    {
        // Arrange
        await Client.AuthenticateAsync();

        // Act
        var response =
            await Client.GetAsync(
                "/api/v1/me/profile");

        // Assert
        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var json =
            await response.Content
                .ReadFromJsonAsync<JsonElement>();

        Assert.Equal(
            "integration-admin@example.com",
            json.GetProperty("email").GetString());
    }

    [Fact]
    public async Task GetProfile_ReturnsProfileForCurrentlyAuthenticatedUser()
    {
        // Arrange
        const string email =
            "profile-user@example.com";

        const string password =
            "ProfileUser123!";

        await Factory.CreateUserAsync(
            email,
            password,
            UserRole.User);

        await Client.AuthenticateAsync(
            UserRole.User,
            email,
            password);

        // Act
        var response =
            await Client.GetAsync(
                "/api/v1/me/profile");

        // Assert
        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var json =
            await response.Content
                .ReadFromJsonAsync<JsonElement>();

        Assert.Equal(
            email,
            json.GetProperty("email").GetString());
    }

    [Fact]
    public async Task SelectRole_WhenRoleIsNotAssigned_ReturnsBadRequest()
    {
        // Arrange
        const string email =
            "regular-user@example.com";

        const string password =
            "RegularUser123!";

        await Factory.CreateUserAsync(
            email,
            password,
            UserRole.User);

        await Client.AuthenticateAsync(
            email: email,
            password: password);

        var request = new SelectRoleViewModel
        {
            RoleCode = UserRole.SuperAdmin
        };

        // Act
        var response =
            await Client.PostAsJsonAsync(
                "/api/v1/me/select-role",
                request);

        // Assert
        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }
}