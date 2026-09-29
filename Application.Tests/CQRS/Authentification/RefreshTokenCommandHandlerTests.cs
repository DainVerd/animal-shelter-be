using Application.CORS.Commands.Authentication;
using Application.Entities;
using Application.Exceptions;
using Application.Interfaces;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using AutoMapper;
using AwesomeAssertions;
using Domain.Models;
using Microsoft.AspNetCore.Http;
using Moq;
using System.Security.Claims;

namespace Application.Tests.CQRS.Authentification;

public class RefreshTokenCommandHandlerTests
{
    private readonly Mock<IUserService> _userServiceMock;
    private readonly Mock<IJwtTokenService> _jwtTokenServiceMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;

    private readonly DefaultHttpContext _httpContext;
    private readonly RefreshTokenCommandHandler _sut;

    public RefreshTokenCommandHandlerTests()
    {
        _userServiceMock = new Mock<IUserService>();
        _jwtTokenServiceMock = new Mock<IJwtTokenService>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _mapperMock = new Mock<IMapper>();
        _httpContextAccessorMock = new Mock<IHttpContextAccessor>();

        _httpContext = new DefaultHttpContext();
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(_httpContext);

        _unitOfWorkMock.Setup(x => x.RefreshTokens).Returns(new Mock<IRefreshTokenRepository>().Object);

        _sut = new RefreshTokenCommandHandler(
            _userServiceMock.Object,
            _jwtTokenServiceMock.Object,
            _unitOfWorkMock.Object,
            _mapperMock.Object,
            _httpContextAccessorMock.Object
        );
    }

    private void SetupMockRepository(Mock<IRefreshTokenRepository> repoMock)
    {
        _unitOfWorkMock.Setup(x => x.RefreshTokens).Returns(repoMock.Object);
    }

    #region Edge Case & Validation Tests

    [Fact]
    public async Task Handle_WhenHttpContextIsNull_ShouldThrowUnauthorizedException()
    {
        // Arrange
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns((HttpContext?)null!);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedException>(() => _sut.Handle(new RefreshTokenCommand(), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_WhenRefreshTokenCookieIsMissing_ThrowUnauthorizedException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedException>(() => _sut.Handle(new RefreshTokenCommand(), CancellationToken.None));
    }

    [Theory]
    [InlineData(true, 10)]  // Revoked, not expired
    [InlineData(false, -10)] // Not revoked, expired
    [InlineData(false, -5)] // Not revoked, but expired
    public async Task Handle_WhenTokenIsRevokedOrExpired_ShouldReturnUnauthorized(bool isRevoked, int minutesDelta)
    {
        // Arrange
        _httpContext.Request.Headers["Cookie"] = "X-Refresh-Token=invalid-token-value";

        var dbToken = new RefreshToken
        {
            Token = "invalid-token-value",
            IsRevoked = isRevoked,
            ExpiresAt = DateTime.UtcNow.AddMinutes(minutesDelta)
        };

        var repoMock = new Mock<IRefreshTokenRepository>();
        repoMock.Setup(x => x.GetRefreshTokenAsync("invalid-token-value", It.IsAny<CancellationToken>()))
            .ReturnsAsync(dbToken);
        SetupMockRepository(repoMock);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedException>(() => _sut.Handle(new RefreshTokenCommand(), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_WhenAssociatedUserIsNotFound_ShouldReturnNotFound()
    {
        // Arrange
        _httpContext.Request.Headers["Cookie"] = "X-Refresh-Token=valid-token";

        var dbToken = new RefreshToken { Token = "valid-token", UserId = 99, IsRevoked = false, ExpiresAt = DateTime.UtcNow.AddDays(1) };
        var repoMock = new Mock<IRefreshTokenRepository>();
        repoMock.Setup(x => x.GetRefreshTokenAsync("valid-token", It.IsAny<CancellationToken>())).ReturnsAsync(dbToken);
        SetupMockRepository(repoMock);

        _userServiceMock.Setup(x => x.GetUserByIdAsync(99)).ReturnsAsync((Domain.Models.User?)null);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => _sut.Handle(new RefreshTokenCommand(), CancellationToken.None));

    }

    #endregion

    #region Success Path Test

    [Fact]
    public async Task Handle_WithValidTokenAndUser_ShouldRotateTokensAndReturnOk()
    {
        // Arrange
        string initialTokenString = "current-refresh-token";
        _httpContext.Request.Headers["Cookie"] = $"X-Refresh-Token={initialTokenString}";

        var storedToken = new RefreshToken
        {
            Token = initialTokenString,
            UserId = 1,
            IsRevoked = false,
            ExpiresAt = DateTime.UtcNow.AddDays(1)
        };

        var repoMock = new Mock<IRefreshTokenRepository>();
        repoMock.Setup(x => x.GetRefreshTokenAsync(initialTokenString, It.IsAny<CancellationToken>()))
            .ReturnsAsync(storedToken);
        SetupMockRepository(repoMock);

        var user = new Domain.Models.User { Id = 1, UserName = "Kurland" };
        _userServiceMock.Setup(x => x.GetUserByIdAsync(1)).ReturnsAsync(user);

        var userClaims = new List<Claim> { new(ClaimTypes.Name, "Kurland") };
        _userServiceMock.Setup(x => x.GetClaimsForAccessTokenByUserIdAsync(1)).ReturnsAsync(userClaims);

        var generatedAccessToken = new TokenResponse { Token = "new-access-token", ExpiresAt = DateTime.UtcNow.AddMinutes(15) };
        var generatedRefreshToken = new TokenResponse { Token = "new-refresh-token", ExpiresAt = DateTime.UtcNow.AddDays(7) };

        // 2. Setup your JWT token service mocks using the correct type signatures
        _jwtTokenServiceMock.Setup(x => x.GenerateAccessToken(userClaims)).Returns(generatedAccessToken);
        _jwtTokenServiceMock.Setup(x => x.GetRefreshToken()).Returns(generatedRefreshToken);

        // 3. Since the production code maps the result, we tell AutoMapper to return our generatedRefreshToken instance
        var mappedNewRefreshToken = new RefreshToken { Token = "new-refresh-token", ExpiresAt = generatedRefreshToken.ExpiresAt };
        _mapperMock.Setup(x => x.Map<RefreshToken>(generatedRefreshToken)).Returns(mappedNewRefreshToken);

        // Act
        var result = await _sut.Handle(new RefreshTokenCommand(), CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.AccessToken.Should().Be("new-access-token");

        // Verify entity updates and transaction state operations
        storedToken.IsRevoked.Should().BeTrue();
        mappedNewRefreshToken.UserId.Should().Be(user.Id);

        repoMock.Verify(x => x.AddAsync(mappedNewRefreshToken, It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

        // Verify the response cookies collection contains our new key assignment
        _httpContext.Response.Headers["Set-Cookie"].ToString().Should().Contain("X-Refresh-Token=new-refresh-token");
    }
    [Fact]
    public async Task Handle_WhenActiveRoleProvidedButUserNotInRole_ShoudlThrowNullReferenceException()
    {
        // Arrange
        string token = "valid-token";
        string role = "Admin";
        _httpContext.Request.Headers["Cookie"] = $"X-Refresh-Token={token}";

        var storedToken = new RefreshToken { Token = token, UserId = 1, IsRevoked = false, ExpiresAt = DateTime.UtcNow.AddDays(1) };
        var repoMock = new Mock<IRefreshTokenRepository>();
        repoMock.Setup(x => x.GetRefreshTokenAsync(token, It.IsAny<CancellationToken>())).ReturnsAsync(storedToken);
        SetupMockRepository(repoMock);

        var user = new Domain.Models.User { Id = 1 };
        _userServiceMock.Setup(x => x.GetUserByIdAsync(1)).ReturnsAsync(user);

        // no roles
        _userServiceMock.Setup(x => x.IsInRoleAsync(user, role)).ReturnsAsync(false);

        // Act
        await Assert.ThrowsAsync<NullReferenceException>(() => _sut.Handle(new RefreshTokenCommand(), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_WithValidActiveRole_ShouldPassRoleToClaimsServiceAndReturnOk()
    {
        // Arrange
        string token = "valid-token";
        string role = "SuperAdmin";
        _httpContext.Request.Headers["Cookie"] = $"X-Refresh-Token={token}";

        var storedToken = new RefreshToken { Token = token, UserId = 1, IsRevoked = false, ExpiresAt = DateTime.UtcNow.AddDays(1) };
        var repoMock = new Mock<IRefreshTokenRepository>();
        repoMock.Setup(x => x.GetRefreshTokenAsync(token, It.IsAny<CancellationToken>())).ReturnsAsync(storedToken);
        SetupMockRepository(repoMock);

        var user = new Domain.Models.User { Id = 1 };
        _userServiceMock.Setup(x => x.GetUserByIdAsync(1)).ReturnsAsync(user);
        _userServiceMock.Setup(x => x.IsInRoleAsync(user, role)).ReturnsAsync(true);

        var claims = new List<Claim> { new(ClaimTypes.Role, role) };

        _userServiceMock.Setup(x => x.GetClaimsForAccessTokenByUserIdAsync(1, role)).ReturnsAsync(claims);

        var genToken = new TokenResponse { Token = "new-access", ExpiresAt = DateTime.UtcNow.AddMinutes(15) };
        var mappedNewRefreshToken = new RefreshToken
        {
            Token = "new-refresh-token",
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        };
        _jwtTokenServiceMock.Setup(x => x.GenerateAccessToken(claims)).Returns(genToken);
        _jwtTokenServiceMock.Setup(x => x.GetRefreshToken()).Returns(new TokenResponse());
        _mapperMock.Setup(x => x.Map<RefreshToken>(It.IsAny<TokenResponse>())).Returns(mappedNewRefreshToken);

        // Act
        var result = await _sut.Handle(new RefreshTokenCommand { ActiveRole = role }, CancellationToken.None);

        // Assert
        _userServiceMock.Verify(x => x.GetClaimsForAccessTokenByUserIdAsync(1, role), Times.Once);
    }
    #endregion
}
