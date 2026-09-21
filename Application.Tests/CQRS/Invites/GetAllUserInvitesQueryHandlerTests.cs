using Application.CORS.Queries;
using Application.Dtos.Invites;
using Application.Entities;
using Application.Entities.Common;
using Application.Interfaces.Services;
using Application.ViewModels;
using AutoMapper;
using Domain.Enums;
using Domain.Models;
using Moq;

namespace Application.Tests.CQRS.Invites;

public class GetAllUserInvitesQueryHandlerTests
{
    private readonly Mock<IUserInviteService> _userInviteServiceMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly GetAllUserInvitesQueryHandler _handler;

    public GetAllUserInvitesQueryHandlerTests()
    {
        _userInviteServiceMock = new Mock<IUserInviteService>();
        _mapperMock = new Mock<IMapper>();

        _handler = new GetAllUserInvitesQueryHandler(
            _userInviteServiceMock.Object,
            _mapperMock.Object);
    }

    [Fact]
    public async Task Handle_ShouldThrowArgumentNullException_WhenPagingIsNull()
    {
        // Arrange
        var query = new GetAllUserInvitesQuery
        {
            Paging = null!,
            Filter = new UserInviteFilterViewModel()
        };

        // Act
        var act = () => _handler.Handle(
            query,
            CancellationToken.None);

        // Assert
        await Assert.ThrowsAsync<ArgumentNullException>(act);

        _userInviteServiceMock.Verify(
            x => x.GetInvitesAsync(
                It.IsAny<UserInviteFilter>(),
                It.IsAny<PaginationParams>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldThrowArgumentNullException_WhenFilterIsNull()
    {
        // Arrange
        var query = new GetAllUserInvitesQuery
        {
            Paging = new PaginationParams
            {
                PageNumber = 1,
                PageSize = 10
            },
            Filter = null!
        };

        // Act
        var act = () => _handler.Handle(
            query,
            CancellationToken.None);

        // Assert
        await Assert.ThrowsAsync<ArgumentNullException>(act);

        _userInviteServiceMock.Verify(
            x => x.GetInvitesAsync(
                It.IsAny<UserInviteFilter>(),
                It.IsAny<PaginationParams>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldRequestInvitationsUsingMappedFilter()
    {
        // Arrange
        var filterViewModel = new UserInviteFilterViewModel
        {
            EmailSearchText = "john@example.com",
            Status = InviteStatus.Pending
        };

        var filter = new UserInviteFilter
        {
            EmailSearchText = "john@example.com",
            Status = InviteStatus.Pending
        };

        var paging = new PaginationParams
        {
            PageNumber = 2,
            PageSize = 10
        };

        _mapperMock
            .Setup(x => x.Map<UserInviteFilter>(filterViewModel))
            .Returns(filter);

        var serviceResult = new PaginatedList<UserInvite>(
            [],
            25,
            paging.PageNumber,
            paging.PageSize);

        _userInviteServiceMock
            .Setup(x => x.GetInvitesAsync(
                filter,
                paging,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(serviceResult);

        var query = new GetAllUserInvitesQuery
        {
            Paging = paging,
            Filter = filterViewModel
        };

        // Act
        await _handler.Handle(
            query,
            CancellationToken.None);

        // Assert
        _mapperMock.Verify(
            x => x.Map<UserInviteFilter>(filterViewModel),
            Times.Once);

        _userInviteServiceMock.Verify(
            x => x.GetInvitesAsync(
                filter,
                paging,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldMapInvitationsAndReturnPaginatedDto()
    {
        // Arrange
        var filterViewModel = new UserInviteFilterViewModel();
        var filter = new UserInviteFilter();

        var paging = new PaginationParams
        {
            PageNumber = 1,
            PageSize = 2
        };

        var invite1 = new UserInvite
        {
            Id = 1,
            Email = "john@example.com",
            Roles = [Application.Constants.UserRole.User],
            Status = InviteStatus.Pending
        };

        var invite2 = new UserInvite
        {
            Id = 2,
            Email = "anna@example.com",
            Roles = [Application.Constants.UserRole.ShelterWorker],
            Status = InviteStatus.Accepted
        };

        var dto1 = new UserInviteDto
        {
            Id = 1,
            Email = "john@example.com",
            Roles = [Application.Constants.UserRole.User],
            Status = InviteStatus.Pending
        };

        var dto2 = new UserInviteDto
        {
            Id = 2,
            Email = "anna@example.com",
            Roles = [Application.Constants.UserRole.ShelterWorker],
            Status = InviteStatus.Accepted
        };

        _mapperMock
            .Setup(x => x.Map<UserInviteFilter>(filterViewModel))
            .Returns(filter);

        _mapperMock
            .Setup(x => x.Map<UserInviteDto>(invite1))
            .Returns(dto1);

        _mapperMock
            .Setup(x => x.Map<UserInviteDto>(invite2))
            .Returns(dto2);

        var serviceResult = new PaginatedList<UserInvite>(
            [invite1, invite2],
            5,
            paging.PageNumber,
            paging.PageSize);

        _userInviteServiceMock
            .Setup(x => x.GetInvitesAsync(
                filter,
                paging,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(serviceResult);

        var query = new GetAllUserInvitesQuery
        {
            Paging = paging,
            Filter = filterViewModel
        };

        // Act
        var result = await _handler.Handle(
            query,
            CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(5, result.TotalCount);
        Assert.Equal(1, result.PageNumber);

        Assert.Equal(2, result.Items.Count);

        Assert.Equal(1, result.Items.First().Id);
        Assert.Equal("john@example.com", result.Items.First().Email);

        Assert.Equal(2, result.Items.ElementAt(1).Id);
        Assert.Equal("anna@example.com", result.Items.ElementAt(1).Email);

        _mapperMock.Verify(
            x => x.Map<UserInviteDto>(invite1),
            Times.Once);

        _mapperMock.Verify(
            x => x.Map<UserInviteDto>(invite2),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldReturnEmptyPage_WhenNoInvitationsExist()
    {
        // Arrange
        var filterViewModel = new UserInviteFilterViewModel();
        var filter = new UserInviteFilter();

        var paging = new PaginationParams
        {
            PageNumber = 1,
            PageSize = 10
        };

        _mapperMock
            .Setup(x => x.Map<UserInviteFilter>(filterViewModel))
            .Returns(filter);

        var serviceResult = new PaginatedList<UserInvite>(
            [],
            0,
            paging.PageNumber,
            paging.PageSize);

        _userInviteServiceMock
            .Setup(x => x.GetInvitesAsync(
                filter,
                paging,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(serviceResult);

        var query = new GetAllUserInvitesQuery
        {
            Paging = paging,
            Filter = filterViewModel
        };

        // Act
        var result = await _handler.Handle(
            query,
            CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result.Items);
        Assert.Equal(0, result.TotalCount);
    }
}
