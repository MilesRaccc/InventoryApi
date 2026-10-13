
using System.Net;
using System.Net.Http.Headers;
using Xunit;

namespace InventoryApi.Tests;

public class ProductsAuthenticationTests
    : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public ProductsAuthenticationTests(
        CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

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
}
