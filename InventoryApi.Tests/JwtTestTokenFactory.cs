
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace InventoryApi.Tests;

internal static class JwtTestTokenFactory
{
    private const string SigningKey =
        "xKDagvH5tpDwN370ckZN0yYG8yC4QYN56rTKKf7IIH4=";

    public static string CreateToken(
        string issuer = "InventoryApi",
        string audience = "InventoryApi.Client",
        DateTime? expires = null,
        string? signingKey = null)
    {
        var actualSigningKey = signingKey ?? SigningKey;

        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(actualSigningKey));

        var credentials = new SigningCredentials(
            key,
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims:
            [
                new Claim(
                    ClaimTypes.NameIdentifier,
                    "test-user-1")
            ],
            expires: expires ?? DateTime.UtcNow.AddMinutes(10),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
