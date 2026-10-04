using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using NotifyApi.Application.DTOs;
using NotifyApi.Domain.Entities;
using NotifyApi.Infrastructure.Persistence;
using NotifyApi.WebApi.Services;
using Xunit;

namespace NotifyApi.UnitTests;

public class SecurityTests
{
    private readonly InMemoryApplicationRepository _appRepo = new();
    private readonly IConfiguration _config;
    private readonly TokenService _tokenService;

    public SecurityTests()
    {
        var inMemorySettings = new Dictionary<string, string?>
        {
            {"Jwt:Secret", "NotifyApi_SuperSecretKey_For_MasterDegree_Posgrado_2026_JWT!"},
            {"Jwt:Issuer", "NotifyApiPlatform"},
            {"Jwt:Audience", "NotifyApiClients"}
        };

        _config = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        _tokenService = new TokenService(_appRepo, _config);
    }

    [Fact]
    public async Task Authenticate_WithValidCredentials_ShouldReturnJwtTokenWithRoleClaims()
    {
        // Arrange
        var request = new TokenRequest
        {
            ClientId = "app_bancamovil_prod",
            ClientSecret = "sec_99a8b7c6d5e4f3a2b1c0"
        };

        // Act
        var response = await _tokenService.AuthenticateAsync(request);

        // Assert
        response.Should().NotBeNull();
        response!.AccessToken.Should().NotBeNullOrWhiteSpace();
        response.TokenType.Should().Be("Bearer");
        response.ExpiresIn.Should().Be(3600);

        // Validar decodificación y claims del token
        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(response.AccessToken);

        jwt.Issuer.Should().Be("NotifyApiPlatform");
        jwt.Audiences.Should().Contain("NotifyApiClients");
        jwt.Claims.Should().Contain(c => c.Type == ClaimTypes.Role && c.Value == "APPLICATION");
        jwt.Claims.Should().Contain(c => c.Type == "tier" && c.Value == "Growth");
    }

    [Fact]
    public async Task Authenticate_WithInvalidSecret_ShouldReturnNull()
    {
        // Arrange
        var request = new TokenRequest
        {
            ClientId = "app_bancamovil_prod",
            ClientSecret = "contrasena_falsa_123"
        };

        // Act
        var response = await _tokenService.AuthenticateAsync(request);

        // Assert
        response.Should().BeNull();
    }
}
