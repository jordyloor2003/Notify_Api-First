using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using NotifyApi.Application.DTOs;
using NotifyApi.Application.Interfaces;

namespace NotifyApi.WebApi.Services;

public class TokenService
{
    private readonly IApplicationRepository _appRepository;
    private readonly IConfiguration _configuration;

    public TokenService(IApplicationRepository appRepository, IConfiguration configuration)
    {
        _appRepository = appRepository;
        _configuration = configuration;
    }

    public async Task<TokenResponse?> AuthenticateAsync(TokenRequest request, CancellationToken ct = default)
    {
        var app = await _appRepository.GetByClientIdAsync(request.ClientId, ct);
        if (app == null || app.ClientSecretHash != request.ClientSecret || !app.IsActive)
        {
            return null;
        }

        var secret = _configuration["Jwt:Secret"] ?? "NotifyApi_SuperSecretKey_For_MasterDegree_Posgrado_2026_JWT!";
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, app.Id.ToString()),
            new Claim("client_id", app.ClientId),
            new Claim(ClaimTypes.Role, app.Role),
            new Claim("tier", app.Tier),
            new Claim("rps_limit", app.RateLimitRps.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"] ?? "NotifyApiPlatform",
            audience: _configuration["Jwt:Audience"] ?? "NotifyApiClients",
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: creds
        );

        return new TokenResponse
        {
            AccessToken = new JwtSecurityTokenHandler().WriteToken(token),
            TokenType = "Bearer",
            ExpiresIn = 3600,
            Scope = "notifications:write notifications:read"
        };
    }
}
