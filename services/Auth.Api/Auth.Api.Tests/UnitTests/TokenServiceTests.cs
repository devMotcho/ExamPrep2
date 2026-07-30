using Auth.Api.Tests.Fixtures;
using Auth.Infrastructure.Security;
using Microsoft.Extensions.Configuration;
using Moq;

namespace Auth.Api.Tests.UnitTests;

public class TokenServiceTests
{
    private readonly JwtTokenService _sut;

    public TokenServiceTests()
    {
        var (privateKey, publicKey) = TestRsaKeys.GenerateTempKeyPair();
        
        var configMock = new Mock<IConfiguration>();
        configMock.Setup(c => c["Jwt:PrivateKeyPath"]).Returns(privateKey);
        configMock.Setup(c => c["Jwt:PublicKeyPath"]).Returns(publicKey);
        
        var rsaKeys = new RsaKeyProvider(configMock.Object);
        _sut = new JwtTokenService(rsaKeys, configMock.Object);
    }

    [Fact]
    public void GenerateOtpCode_Returns8DigitNumber()
    {
        var code = _sut.GenerateOtpCode();

        Assert.NotNull(code);
        Assert.Equal(8, code.Length);
        Assert.True(int.TryParse(code, out _), "Code must be numeric");
    }

    [Fact]
    public void HashOtpCode_ReturnsConsistentHash()
    {
        var code = "12345678";
        
        var hash1 = _sut.HashOtpCode(code);
        var hash2 = _sut.HashOtpCode(code);

        Assert.NotNull(hash1);
        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public void GenerateResetTicket_ReturnsValidBase64String()
    {
        var ticket = _sut.GenerateResetTicket();

        Assert.NotNull(ticket);
        Assert.True(ticket.Length > 20); // Should be a long string (64 bytes in base64 is 88 chars)
        
        // Assert it's valid base64
        var bytes = Convert.FromBase64String(ticket);
        Assert.Equal(64, bytes.Length);
    }

    [Fact]
    public void HashResetTicket_ValidBase64_ReturnsConsistentHash()
    {
        var ticket = _sut.GenerateResetTicket();
        
        var hash1 = _sut.HashResetTicket(ticket);
        var hash2 = _sut.HashResetTicket(ticket);

        Assert.NotNull(hash1);
        Assert.NotEqual(string.Empty, hash1);
        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public void HashResetTicket_InvalidBase64_ReturnsEmptyString()
    {
        var hash = _sut.HashResetTicket("not-valid-base64!!");

        Assert.Equal(string.Empty, hash);
    }
}
