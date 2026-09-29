using System.Net;
using System.Net.Http.Json;
using Application.Constants;
using Microsoft.AspNetCore.Mvc.Rendering;
using WebApi.IntegrationTests.Extensions;
using WebApi.IntegrationTests.Infrastructure;

namespace WebApi.IntegrationTests.Controllers;

public class LookupControllerTests
    : IntegrationTestBase
{
    public LookupControllerTests(
        CustomWebApplicationFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task GetEnumOptions_WithoutAuthentication_ReturnsUnauthorized()
    {
        var response =
            await Client.GetAsync(
                "/api/v1/lookup/gender");

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    [Fact]
    public async Task GetGenderOptions_ReturnsExpectedValues()
    {
        // Arrange
        await Client.AuthenticateAsync();

        // Act
        var response =
            await Client.GetAsync(
                "/api/v1/lookup/gender");

        // Assert
        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var result =
            await response.Content
                .ReadFromJsonAsync<List<SelectListItem>>();

        Assert.NotNull(result);

        Assert.Collection(
            result,
            item =>
            {
                Assert.Equal("0", item.Value);
                Assert.Equal("Male", item.Text);
            },
            item =>
            {
                Assert.Equal("1", item.Value);
                Assert.Equal("Female", item.Text);
            });
    }

    [Fact]
    public async Task GetSizeOptions_ReturnsExpectedValues()
    {
        await Client.AuthenticateAsync();

        var response =
            await Client.GetAsync(
                "/api/v1/lookup/size");

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var result =
            await response.Content
                .ReadFromJsonAsync<List<SelectListItem>>();

        Assert.NotNull(result);

        Assert.Collection(
            result,
            item =>
            {
                Assert.Equal("0", item.Value);
                Assert.Equal("Small", item.Text);
            },
            item =>
            {
                Assert.Equal("1", item.Value);
                Assert.Equal("Medium", item.Text);
            },
            item =>
            {
                Assert.Equal("2", item.Value);
                Assert.Equal("Large", item.Text);
            });
    }

    [Fact]
    public async Task GetTemperamentOptions_ReturnsExpectedValues()
    {
        await Client.AuthenticateAsync();

        var response =
            await Client.GetAsync(
                "/api/v1/lookup/temperament");

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var result =
            await response.Content
                .ReadFromJsonAsync<List<SelectListItem>>();

        Assert.NotNull(result);

        var texts =
            result.Select(x => x.Text).ToList();

        Assert.Contains("Active", texts);
        Assert.Contains("Calm", texts);
        Assert.Contains("Timid", texts);
        Assert.Contains("Friendly", texts);
        Assert.Contains("Independent", texts);

        Assert.Equal(5, result.Count);
    }

    [Fact]
    public async Task GetInviteStatusOptions_ReturnsExpectedNumericValues()
    {
        await Client.AuthenticateAsync();

        var response =
            await Client.GetAsync(
                "/api/v1/lookup/invitestatus");

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var result =
            await response.Content
                .ReadFromJsonAsync<List<SelectListItem>>();

        Assert.NotNull(result);

        Assert.Collection(
            result,
            item =>
            {
                Assert.Equal("1", item.Value);
                Assert.Equal("Pending", item.Text);
            },
            item =>
            {
                Assert.Equal("2", item.Value);
                Assert.Equal("Accepted", item.Text);
            },
            item =>
            {
                Assert.Equal("3", item.Value);
                Assert.Equal("Expired", item.Text);
            },
            item =>
            {
                Assert.Equal("4", item.Value);
                Assert.Equal("Revoked", item.Text);
            });
    }

    [Fact]
    public async Task GetEnumOptions_WithUnknownEnum_ReturnsNotFound()
    {
        await Client.AuthenticateAsync();

        var response =
            await Client.GetAsync(
                "/api/v1/lookup/does-not-exist");

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);
    }

    #region GetAvailableInviteRoles
    [Fact]
    public async Task GetAvailableInviteRoles_WithSuperAdmin_ReturnsAllRoles()
    {
        // Arrange
        await Client.AuthenticateAsync(
            UserRole.SuperAdmin);

        // Act
        var response =
            await Client.GetAsync(
                "/api/v1/lookup/user-roles");

        // Assert
        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var result =
            await response.Content
                .ReadFromJsonAsync<List<SelectListItem>>();

        Assert.NotNull(result);
        Assert.NotEmpty(result);

        var roleNames =
            result.Select(x => x.Text).ToList();

        Assert.Contains(
            UserRole.SuperAdmin,
            roleNames);

        Assert.Contains(
            UserRole.Admin,
            roleNames);

        Assert.Contains(
            UserRole.ShelterWorker,
            roleNames);

        Assert.Contains(
            UserRole.User,
            roleNames);
    }

    [Fact]
    public async Task GetAvailableInviteRoles_WithAdmin_ReturnsUserAndShelterWorker()
    {
        // Arrange
        const string email =
            "integration-admin-role@example.com";

        const string password =
            "TestAdminRole123!";

        await Factory.CreateUserAsync(
            email,
            password,
            UserRole.Admin);

        await Client.AuthenticateAsync(
            UserRole.Admin,
            email,
            password);

        // Act
        var response =
            await Client.GetAsync(
                "/api/v1/lookup/user-roles");

        // Assert
        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var result =
            await response.Content
                .ReadFromJsonAsync<List<SelectListItem>>();

        Assert.NotNull(result);

        var roleNames =
            result.Select(x => x.Text).ToList();

        Assert.Equal(2, roleNames.Count);

        Assert.Contains(
            UserRole.User,
            roleNames);

        Assert.Contains(
            UserRole.ShelterWorker,
            roleNames);

        Assert.DoesNotContain(
            UserRole.Admin,
            roleNames);

        Assert.DoesNotContain(
            UserRole.SuperAdmin,
            roleNames);
    }

    [Fact]
    public async Task GetAvailableInviteRoles_WithUser_ReturnsEmptyList()
    {
        // Arrange
        const string email =
            "integration-user@example.com";

        const string password =
            "TestUser123!";

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
                "/api/v1/lookup/user-roles");

        // Assert
        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var result =
            await response.Content
                .ReadFromJsonAsync<List<SelectListItem>>();

        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetAvailableInviteRoles_WithoutAuthentication_ReturnsUnauthorized()
    {
        var response =
            await Client.GetAsync(
                "/api/v1/lookup/user-roles");

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }
    #endregion
}