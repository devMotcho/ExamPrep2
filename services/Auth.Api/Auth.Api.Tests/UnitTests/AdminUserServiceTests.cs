using Auth.Application.Interfaces;
using Auth.Application.Results;
using Auth.Application.Services;
using Auth.Domain.Rules;
using Auth.Application.Models;
using Moq;

namespace Auth.Api.Tests.UnitTests;

public class AdminUserServiceTests
{
    private readonly Mock<IUserRepository> _usersMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly AdminUserService _sut;

    public AdminUserServiceTests()
    {
        _sut = new AdminUserService(_usersMock.Object, _unitOfWorkMock.Object);
    }

    [Fact]
    public async Task AssignRole_ReturnsUnknownRole_WhenRoleInvalid()
    {
        var result = await _sut.AssignRoleAsync("user-id", "InvalidRole");
        Assert.Equal(AssignRoleStatus.UnknownRole, result.Status);
    }

    [Fact]
    public async Task AssignRole_ReturnsUserNotFound_WhenUserDoesNotExist()
    {
        _usersMock.Setup(u => u.FindByIdAsync("user-id")).ReturnsAsync((AppUser?)null);
        var result = await _sut.AssignRoleAsync("user-id", Roles.Admin);
        Assert.Equal(AssignRoleStatus.UserNotFound, result.Status);
    }

    [Fact]
    public async Task AssignRole_Succeeds_WhenRoleValidAndUserExists()
    {
        _usersMock.Setup(u => u.FindByIdAsync("user-id")).ReturnsAsync(new AppUser("user-id", "test@test.com", DateTime.UtcNow, new[] { Roles.Student }));
        var result = await _sut.AssignRoleAsync("user-id", Roles.Promoter);

        Assert.Equal(AssignRoleStatus.Success, result.Status);
        _usersMock.Verify(u => u.AddToRoleAsync("user-id", Roles.Promoter), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task RemoveRole_ReturnsRoleIsProtected_WhenRemovingStudent()
    {
        var result = await _sut.RemoveRoleAsync("user-id", Roles.Student);
        Assert.Equal(RemoveRoleStatus.RoleIsProtected, result.Status);
    }

    [Fact]
    public async Task RemoveRole_ReturnsLastAdminCannotBeRemoved_WhenOnlyOneAdminExists()
    {
        _usersMock.Setup(u => u.FindByIdAsync("user-id")).ReturnsAsync(new AppUser("user-id", "test@test.com", DateTime.UtcNow, new[] { Roles.Student, Roles.Admin }));
        _usersMock.Setup(u => u.CountUsersInRoleAsync(Roles.Admin)).ReturnsAsync(1);

        var result = await _sut.RemoveRoleAsync("user-id", Roles.Admin);
        
        Assert.Equal(RemoveRoleStatus.LastAdminCannotBeRemoved, result.Status);
        _usersMock.Verify(u => u.RemoveFromRoleAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task RemoveRole_Succeeds_WhenValid()
    {
        _usersMock.Setup(u => u.FindByIdAsync("user-id")).ReturnsAsync(new AppUser("user-id", "test@test.com", DateTime.UtcNow, new[] { Roles.Student, Roles.Promoter }));

        var result = await _sut.RemoveRoleAsync("user-id", Roles.Promoter);
        
        Assert.Equal(RemoveRoleStatus.Success, result.Status);
        _usersMock.Verify(u => u.RemoveFromRoleAsync("user-id", Roles.Promoter), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }
}
