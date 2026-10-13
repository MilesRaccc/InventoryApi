using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using InventoryApi.Data;
using InventoryApi.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using System.Text;

namespace InventoryApi.Tests;

public class AuthenticationTests
    : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    public AuthenticationTests(
        CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    #region JWT Tests

    [Fact]
    public async Task GetProducts_WithoutToken_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/products");

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    [Fact]
    public async Task GetProducts_WithValidToken_DoesNotReturnUnauthorized()
    {
        var token = JwtTestTokenFactory.CreateToken();

        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync("/api/products");

        Assert.NotEqual(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    [Fact]
    public async Task GetProducts_WithInvalidSignature_ReturnsUnauthorized()
    {
        var token = JwtTestTokenFactory.CreateToken(
            signingKey: "xDDagvH5tpDwN370ckZN0yYG8yC4QYN56rTKKf7IIH4=");

        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync("/api/products");

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    [Fact]
    public async Task GetProducts_WithExpiredToken_ReturnsUnauthorized()
    {
        var token = JwtTestTokenFactory.CreateToken(
            expires: DateTime.UtcNow.AddMinutes(-5));

        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync("/api/products");

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    #endregion


    #region Identity Tests

    private static string UniqueEmail() =>
        $"test-{Guid.NewGuid():N}@example.com";

    private const string TestPassword = "TestPass123";

    private async Task EnsureUserRoleExistsAsync()
    {
        using var scope = _factory.Services.CreateScope();

        var roleManager = scope.ServiceProvider
            .GetRequiredService<RoleManager<IdentityRole>>();

        if (!await roleManager.RoleExistsAsync("User"))
        {
            var result = await roleManager.CreateAsync(
                new IdentityRole("User"));

            Assert.True(
                result.Succeeded,
                string.Join("; ", result.Errors.Select(e => e.Description)));
        }
    }

    [Fact]
    public async Task Register_ValidData_CreatesUserWithUserRole()
    {
        await EnsureUserRoleExistsAsync();

        var email = UniqueEmail();

        var response = await _client.PostAsJsonAsync(
            "/api/Auth/register",
            new
            {
                Email = email,
                Password = TestPassword
            });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        using var scope = _factory.Services.CreateScope();

        var userManager = scope.ServiceProvider
            .GetRequiredService<UserManager<ApplicationUser>>();

        var user = await userManager.FindByEmailAsync(email);

        Assert.NotNull(user);
        Assert.True(await userManager.IsInRoleAsync(user, "User"));
        Assert.False(await userManager.IsInRoleAsync(user, "Admin"));
    }

    [Fact]
    public async Task Register_DuplicateEmail_ReturnsBadRequest()
    {
        await EnsureUserRoleExistsAsync();

        var email = UniqueEmail();

        var payload = new
        {
            Email = email,
            Password = TestPassword
        };

        var firstResponse = await _client.PostAsJsonAsync(
            "/api/Auth/register", payload);

        var secondResponse = await _client.PostAsJsonAsync(
            "/api/Auth/register", payload);

        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, secondResponse.StatusCode);
    }

    [Fact]
    public async Task Login_ValidCredentials_ReturnsJwt()
    {
        await EnsureUserRoleExistsAsync();

        var email = UniqueEmail();

        var registerResponse = await _client.PostAsJsonAsync(
            "/api/Auth/register",
            new
            {
                Email = email,
                Password = TestPassword
            });

        Assert.Equal(HttpStatusCode.Created, registerResponse.StatusCode);

        var response = await _client.PostAsJsonAsync(
            "/api/Auth/login",
            new
            {
                Email = email,
                Password = TestPassword
            });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var json = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync());

        Assert.True(
            json.RootElement.TryGetProperty("accessToken", out var token)
            || json.RootElement.TryGetProperty("AccessToken", out token));

        Assert.False(string.IsNullOrWhiteSpace(token.GetString()));
    }

    [Fact]
    public async Task Login_InvalidPassword_ReturnsUnauthorized()
    {
        await EnsureUserRoleExistsAsync();

        var email = UniqueEmail();

        var registerResponse = await _client.PostAsJsonAsync(
            "/api/Auth/register",
            new
            {
                Email = email,
                Password = TestPassword
            });

        Assert.Equal(HttpStatusCode.Created, registerResponse.StatusCode);

        var response = await _client.PostAsJsonAsync(
            "/api/Auth/login",
            new
            {
                Email = email,
                Password = "WrongPass123"
            });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    #endregion

}
