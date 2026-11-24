using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Habitarium.Api.Models;
using Microsoft.IdentityModel.Tokens;

namespace Habitarium.Api.Services;

public class TokenService(IConfiguration config)
{
    public (string Token, DateTime ExpiresAt) Create(AppUser user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Key"]!));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expiresAt = DateTime.UtcNow.AddMinutes(config.GetValue<int>("Jwt:ExpiresMinutes", 1440));

        var claims = new[]
        {
            new Claim("sub", user.Id.ToString()),
            new Claim("email", user.Email),
            new Claim("name", user.Name)
        };

        var token = new JwtSecurityToken(
            issuer: config["Jwt:Issuer"],
            audience: config["Jwt:Audience"],
            claims: claims,
            expires: expiresAt,
            signingCredentials: credentials);

        return (new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }
}