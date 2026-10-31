using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Application.Abstractions.Authentication;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Infrastructure.Authentication;

public sealed class JwtTokenProvider(
    IOptions<JwtOptions> options
    ) : ITokenProvider
{
    public AccessToken CreateToken(Guid userId)
    {
        var settings = options.Value;

        var expiresAt = DateTime.UtcNow
            .AddMinutes(settings.ExpirationMinutes);
        
        var identity = new ClaimsIdentity();
        
        identity.AddClaim(new Claim("sub", userId.ToString()));
        identity.AddClaim(new Claim("jti", Guid.NewGuid().ToString()));
        
        var keyBytes = Convert.FromBase64String(settings.SigningKey);
        var securityKey = new SymmetricSecurityKey(keyBytes);
        
        var signingCredentials = new SigningCredentials(
            securityKey,
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: settings.Issuer,
            audience: settings.Audience,
            claims: identity.Claims,
            expires: expiresAt,
            signingCredentials: signingCredentials
        );
        
        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

        return new AccessToken(tokenString, expiresAt);
    }
}