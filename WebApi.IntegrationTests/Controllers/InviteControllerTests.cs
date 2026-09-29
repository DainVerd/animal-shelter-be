using Application.Constants;
using Application.CORS.Commands;
using Application.Dtos;
using Application.Dtos.Invites;
using Application.Entities.Common;
using Application.ViewModels.User;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using WebApi.IntegrationTests.Extensions;
using WebApi.IntegrationTests.Fakes;
using WebApi.IntegrationTests.Infrastructure;
using WebApi.IntegrationTests.Models;

namespace WebApi.IntegrationTests.Controllers;

public class InviteControllerTests
    : IntegrationTestBase
{
    public InviteControllerTests(
        CustomWebApplicationFactory factory)
        : base(factory)
    {

    }

    [Fact]
    public async Task SendInvite_WithoutAuthentication_ReturnsUnauthorized()
    {
        var request = new SendInviteViewModel
        {
            Email = "new-user@example.com",
            Roles = [UserRole.User]
        };

        var response = await Client.PostAsJsonAsync(
            "/api/v1/invites",
            request);

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    [Fact]
    public async Task SendInvite_WithUserRole_ReturnsForbidden()
    {
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

        var request = new SendInviteViewModel
        {
            Email = "invited@example.com",
            Roles = [UserRole.User]
        };

        var response = await Client.PostAsJsonAsync(
            "/api/v1/invites",
            request);

        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);
    }

    [Fact]
    public async Task SendInvite_WithSuperAdmin_ReturnsCreated()
    {
        // Arrange
        await Client.AuthenticateAsync(
            UserRole.SuperAdmin);

        var request = new SendInviteViewModel
        {
            Email = "invited@example.com",
            Roles = [UserRole.User]
        };

        // Act
        var response = await Client.PostAsJsonAsync(
            "/api/v1/invites",
            request);

        // Assert
        Assert.Equal(
            HttpStatusCode.Created,
            response.StatusCode);

        var inviteId =
            await response.Content.ReadFromJsonAsync<int>();

        Assert.True(inviteId > 0);

        var emailService =
            Factory.Services
                .GetRequiredService<FakeEmailService>();

        Assert.Contains(
            emailService.SentTemplateEmails,
            x => x.ToEmail == request.Email);
    }

    [Fact]
    public async Task SendInvite_WhenUserAlreadyExists_ReturnsConflict()
    {
        // Arrange
        const string email =
            "existing@example.com";

        await Factory.CreateUserAsync(
            email,
            "ExistingUser123!",
            UserRole.User);

        await Client.AuthenticateAsync(
            UserRole.SuperAdmin);

        var request = new SendInviteViewModel
        {
            Email = email,
            Roles = [UserRole.User]
        };

        // Act
        var response = await Client.PostAsJsonAsync(
            "/api/v1/invites",
            request);

        // Assert
        Assert.Equal(
            HttpStatusCode.Conflict,
            response.StatusCode);
    }

    [Fact]
    public async Task SendInvite_WhenPendingInviteAlreadyExists_ReturnsConflict()
    {
        // Arrange
        await Client.AuthenticateAsync(
            UserRole.SuperAdmin);

        var request = new SendInviteViewModel
        {
            Email = "duplicate@example.com",
            Roles = [UserRole.User]
        };

        var firstResponse =
            await Client.PostAsJsonAsync(
                "/api/v1/invites",
                request);

        Assert.Equal(
            HttpStatusCode.Created,
            firstResponse.StatusCode);

        // Act
        var secondResponse =
            await Client.PostAsJsonAsync(
                "/api/v1/invites",
                request);

        // Assert
        Assert.Equal(
            HttpStatusCode.Conflict,
            secondResponse.StatusCode);
    }

    [Fact]
    public async Task SendInvite_ThenAcceptInvite_ReturnsAccessToken()
    {
        // Arrange
        const string invitedEmail =
            "accepted-user@example.com";

        await Client.AuthenticateAsync(
            UserRole.SuperAdmin);

        var sendRequest = new SendInviteViewModel
        {
            Email = invitedEmail,
            Roles = [UserRole.User]
        };

        var sendResponse =
            await Client.PostAsJsonAsync(
                "/api/v1/invites",
                sendRequest);

        Assert.Equal(
            HttpStatusCode.Created,
            sendResponse.StatusCode);

        var emailService =
            Factory.Services
                .GetRequiredService<FakeEmailService>();

        var token =
            emailService.GetInviteToken(invitedEmail);

        Client.DefaultRequestHeaders.Authorization = null;

        var acceptRequest =
            new AcceptInviteCommand
            {
                Token = token,
                Password = "AcceptedUser123!"
            };

        // Act
        var response =
            await Client.PostAsJsonAsync(
                "/api/v1/invites/accept",
                acceptRequest);

        // Assert
        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var result =
            await response.Content
                .ReadFromJsonAsync<SignInResponseDto>();

        Assert.NotNull(result);

        Assert.False(
            string.IsNullOrWhiteSpace(
                result.AccessToken));

        Assert.True(
            result.AccessTokenExpiresAt >
            DateTime.UtcNow);
    }

    [Fact]
    public async Task AcceptInvite_WithInvalidToken_ReturnsNotFound()
    {
        var request = new AcceptInviteCommand
        {
            Token = "completely-invalid-token",
            Password = "ValidPassword123!"
        };

        var response =
            await Client.PostAsJsonAsync(
                "/api/v1/invites/accept",
                request);

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);
    }

    [Fact]
    public async Task AcceptInvite_WhenAlreadyAccepted_ReturnsConflict()
    {
        // Arrange
        const string email =
            "accept-twice@example.com";

        await Client.AuthenticateAsync(
            UserRole.SuperAdmin);

        var sendResponse =
            await Client.PostAsJsonAsync(
                "/api/v1/invites",
                new SendInviteViewModel
                {
                    Email = email,
                    Roles = [UserRole.User]
                });

        Assert.Equal(
            HttpStatusCode.Created,
            sendResponse.StatusCode);

        var emailService =
            Factory.Services
                .GetRequiredService<FakeEmailService>();

        var token =
            emailService.GetInviteToken(email);

        Client.DefaultRequestHeaders.Authorization = null;

        var request = new AcceptInviteCommand
        {
            Token = token,
            Password = "AcceptedUser123!"
        };

        var firstResponse =
            await Client.PostAsJsonAsync(
                "/api/v1/invites/accept",
                request);

        Assert.Equal(
            HttpStatusCode.OK,
            firstResponse.StatusCode);

        // Act
        var secondResponse =
            await Client.PostAsJsonAsync(
                "/api/v1/invites/accept",
                request);

        // Assert
        Assert.Equal(
            HttpStatusCode.Conflict,
            secondResponse.StatusCode);
    }

    #region GET invites
    [Fact]
    public async Task GetInvites_WithoutAuthentication_ReturnsUnauthorized()
    {
        var response =
            await Client.GetAsync(
                "/api/v1/invites");

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }
    [Fact]
    public async Task GetInvites_WithUserRole_ReturnsForbidden()
    {
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

        var response =
            await Client.GetAsync(
                "/api/v1/invites");

        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);
    }
    [Fact]
    public async Task GetInvites_WithSuperAdmin_ReturnsOk()
    {
        await Client.AuthenticateAsync(
            UserRole.SuperAdmin);

        var response =
            await Client.GetAsync(
                "/api/v1/invites?pageNumber=1&pageSize=10");

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var result =
            await response.Content
                .ReadFromJsonAsync<
                    PaginatedResponse<UserInviteDto>>();

        Assert.NotNull(result);
    }

    [Fact]
    public async Task GetInvites_ReturnsCreatedInviteOnFirstPage()
    {
        // Arrange
        await Client.AuthenticateAsync(
            UserRole.SuperAdmin);

        const string email =
            "first-page@example.com";

        var createResponse =
            await Client.PostAsJsonAsync(
                "/api/v1/invites",
                new SendInviteViewModel
                {
                    Email = email,
                    Roles = [UserRole.User]
                });

        Assert.Equal(
            HttpStatusCode.Created,
            createResponse.StatusCode);

        // Act
        var response =
            await Client.GetAsync(
                "/api/v1/invites?pageNumber=1&pageSize=10");

        // Assert
        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var result =
            await response.Content
                .ReadFromJsonAsync<
                    PaginatedResponse<UserInviteDto>>();

        Assert.NotNull(result);

        Assert.Contains(
            result.Items,
            x => x.Email == email);
    }
    #endregion
}