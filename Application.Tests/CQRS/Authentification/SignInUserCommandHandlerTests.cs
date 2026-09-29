using Application.CORS.Commands.Authentication;
using Application.Entities;
using Application.Exceptions;
using Application.Interfaces;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Application.ViewModels.Authentication;
using AwesomeAssertions;
using Domain.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;

namespace Application.Tests.CQRS.Authentification;

public class SignInUserCommandHandlerTests
{
    private readonly Mock<IUserService> _userServiceMock;
    private readonly Mock<IJwtTokenService> _jwtTokenServiceMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;
    private readonly Mock<ILogger<SignInUserCommand>> _loggerMock;
    private readonly Mock<IRefreshTokenRepository> _refreshTokenRepoMock;

    private readonly DefaultHttpContext _httpContext;
    private readonly SignInUserCommandHandler _sut;

    public SignInUserCommandHandlerTests()
    {
        _userServiceMock = new Mock<IUserService>();
        _jwtTokenServiceMock = new Mock<IJwtTokenService>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        _loggerMock = new Mock<ILogger<SignInUserCommand>>();
        _refreshTokenRepoMock = new Mock<IRefreshTokenRepository>();

        // Setup real HttpContext for response cookie parsing logic
        _httpContext = new DefaultHttpContext();
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(_httpContext);

        // Bind the repository mock to the Unit of Work
        _unitOfWorkMock.Setup(x => x.RefreshTokens).Returns(_refreshTokenRepoMock.Object);

        _sut = new SignInUserCommandHandler(
            _userServiceMock.Object,
            _jwtTokenServiceMock.Object,
            _unitOfWorkMock.Object,
            _httpContextAccessorMock.Object,
            _loggerMock.Object
        );
    }

    private SignInUserCommand CreateCommand(string email = "test@user.com", string password = "Password123!")
    {
        return new SignInUserCommand
        {
            Model = new SignInViewModel
            {
                Email = email,
                Password = password
            }
        };
    }

    #region Fail Path & Boundary Tests

    [Fact]
    public async Task Handle_WhenUserDoesNotExist_ShouldReturnNotFound()
    {
        // Arrange
        var command = CreateCommand();
        _userServiceMock.Setup(x => x.GetUserByEmailAsync(command.Model.Email))
            .ReturnsAsync((Domain.Models.User?)null);

        // Act & Assert
        await Assert.ThrowsAsync<Exceptions.NotFoundException>(() => _sut.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_WhenHttpContextIsNull_ShouldReturnNotFound()
    {
        // Arrange
        var command = CreateCommand();
        var user = new Domain.Models.User { Id = 1, Email = command.Model.Email };

        _userServiceMock.Setup(x => x.GetUserByEmailAsync(command.Model.Email)).ReturnsAsync(user);
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns((HttpContext?)null!);

        // Act & Assert
        await Assert.ThrowsAsync<Exceptions.ValidationException>(() => _sut.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_WhenUserIsLockedOut_ShouldLogWarningAndReturnUnauthorized()
    {
        // Arrange
        var command = CreateCommand();
        var user = new Domain.Models.User { Id = 1, Email = command.Model.Email };

        _userServiceMock.Setup(x => x.GetUserByEmailAsync(command.Model.Email)).ReturnsAsync(user);
        _userServiceMock.Setup(x => x.IsLockedOutAsync(user)).ReturnsAsync(true);

        // Act & Assert
        await Assert.ThrowsAsync<ApplicationException>(() => _sut.Handle(command, CancellationToken.None));

        // Verify Logger warning statement execution sequence 
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"User {command.Model.Email} is locked out.")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenPasswordIsIncorrect_ShouldIncrementAccessFailedAndReturnBadRequest()
    {
        // Arrange
        var command = CreateCommand();
        var user = new Domain.Models.User { Id = 1, Email = command.Model.Email };

        _userServiceMock.Setup(x => x.GetUserByEmailAsync(command.Model.Email)).ReturnsAsync(user);
        _userServiceMock.Setup(x => x.IsLockedOutAsync(user)).ReturnsAsync(false);
        _userServiceMock.Setup(x => x.CheckPasswordAsync(user, command.Model.Password)).ReturnsAsync(false);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedException>(() => _sut.Handle(command, CancellationToken.None));
        _userServiceMock.Verify(x => x.IncrementAccessFailedCountAsync(user), Times.Once);
        _userServiceMock.Verify(x => x.ResetAccessFailedCountAsync(user), Times.Never);
    }

    #endregion

    #region Success Path Test

    [Fact]
    public async Task Handle_WithValidCredentials_ShouldResetCounterSaveTokenAndReturnOk()
    {
        // Arrange
        var command = CreateCommand();
        var user = new Domain.Models.User { Id = 42, Email = command.Model.Email };
        var claims = new List<Claim> { new(ClaimTypes.Email, user.Email) };

        _userServiceMock.Setup(x => x.GetUserByEmailAsync(command.Model.Email)).ReturnsAsync(user);
        _userServiceMock.Setup(x => x.IsLockedOutAsync(user)).ReturnsAsync(false);
        _userServiceMock.Setup(x => x.CheckPasswordAsync(user, command.Model.Password)).ReturnsAsync(true);
        _userServiceMock.Setup(x => x.GetClaimsForAccessTokenByUserIdAsync(user.Id)).ReturnsAsync(claims);

        var outAccessToken = new TokenResponse { Token = "valid-access-token", ExpiresAt = DateTime.UtcNow.AddMinutes(15) };
        var outRefreshToken = new TokenResponse { Token = "valid-refresh-token", ExpiresAt = DateTime.UtcNow.AddDays(7) };

        _jwtTokenServiceMock.Setup(x => x.GenerateAccessToken(claims)).Returns(outAccessToken);
        _jwtTokenServiceMock.Setup(x => x.GetRefreshToken()).Returns(outRefreshToken);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.AccessToken.Should().Be("valid-access-token");

        // Verify side effects
        _userServiceMock.Verify(x => x.ResetAccessFailedCountAsync(user), Times.Once);

        _refreshTokenRepoMock.Verify(x => x.Add(It.Is<RefreshToken>(r =>
            r.Token == "valid-refresh-token" &&
            r.UserId == user.Id &&
            !r.IsRevoked
        )), Times.Once);

        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

        // Verify that response headers cookie tracking sets up properly
        _httpContext.Response.Headers["Set-Cookie"].ToString().Should().Contain("X-Refresh-Token=valid-refresh-token");
    }

    #endregion
}
