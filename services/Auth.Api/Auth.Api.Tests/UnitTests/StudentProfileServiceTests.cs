using Auth.Application.Interfaces;
using Auth.Application.Results;
using Auth.Application.Services;
using Auth.Application.Models;
using Moq;
using Microsoft.AspNetCore.Identity;

namespace Auth.Api.Tests.UnitTests;

public class StudentProfileServiceTests
{
    private readonly Mock<IUserRepository> _usersMock = new();
    private readonly Mock<IRefreshTokenRepository> _refreshTokensMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly StudentProfileService _sut;

    public StudentProfileServiceTests()
    {
        _sut = new StudentProfileService(_usersMock.Object, _refreshTokensMock.Object, _unitOfWorkMock.Object);
    }

    [Fact]
    public async Task UpdateProfile_ReturnsValidationFailed_WhenUserNotFound()
    {
        _usersMock.Setup(u => u.FindByIdAsync("user-id")).ReturnsAsync((AppUser?)null);
        var result = await _sut.UpdateProfileAsync("user-id", "John", "Doe", null);
        Assert.Equal(UpdateProfileStatus.ValidationFailed, result.Status);
    }

    [Fact]
    public async Task UpdateProfile_ReturnsSuccess_WhenValid()
    {
        _usersMock.Setup(u => u.FindByIdAsync("user-id")).ReturnsAsync(new AppUser("user-id", "test@test.com", DateTime.UtcNow, []));
        _usersMock.Setup(u => u.UpdateProfileAsync("user-id", "John", "Doe", null))
                  .ReturnsAsync(IdentityResult.Success);

        var result = await _sut.UpdateProfileAsync("user-id", "John", "Doe", null);
        Assert.Equal(UpdateProfileStatus.Success, result.Status);
    }

    [Fact]
    public async Task ChangePassword_ReturnsIncorrectPassword_WhenCurrentIsWrong()
    {
        _usersMock.Setup(u => u.FindByIdAsync("user-id")).ReturnsAsync(new AppUser("user-id", "test@test.com", DateTime.UtcNow, []));
        
        var identityError = new IdentityError { Code = "PasswordMismatch", Description = "Incorrect password" };
        _usersMock.Setup(u => u.ChangePasswordAsync("user-id", "wrong", "newPass"))
                  .ReturnsAsync(IdentityResult.Failed(identityError));

        var result = await _sut.ChangePasswordAsync("user-id", "wrong", "newPass");
        Assert.Equal(ChangePasswordStatus.IncorrectCurrentPassword, result.Status);
    }

    [Fact]
    public async Task ChangePassword_RevokesTokensAndSaves_WhenSuccessful()
    {
        _usersMock.Setup(u => u.FindByIdAsync("user-id")).ReturnsAsync(new AppUser("user-id", "test@test.com", DateTime.UtcNow, []));
        _usersMock.Setup(u => u.ChangePasswordAsync("user-id", "correct", "newPass"))
                  .ReturnsAsync(IdentityResult.Success);

        var result = await _sut.ChangePasswordAsync("user-id", "correct", "newPass");
        
        Assert.Equal(ChangePasswordStatus.Success, result.Status);
        _refreshTokensMock.Verify(r => r.RevokeAllForUserAsync("user-id"), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task Deactivate_RevokesTokensAndDeactivates_WhenUserExists()
    {
        _usersMock.Setup(u => u.FindByIdAsync("user-id")).ReturnsAsync(new AppUser("user-id", "test@test.com", DateTime.UtcNow, []));
        
        await _sut.DeactivateAsync("user-id");

        _usersMock.Verify(u => u.DeactivateAsync("user-id"), Times.Once);
        _refreshTokensMock.Verify(r => r.RevokeAllForUserAsync("user-id"), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }
}
