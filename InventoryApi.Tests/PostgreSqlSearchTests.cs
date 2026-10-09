using System.Net;
using System.Net.Http.Json;
using InventoryApi.Data;
using InventoryApi.Dtos.Product;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace InventoryApi.Tests;

public class PostgreSqlSearchTests
{
    [Fact]
    public async Task GetProducts_SearchIsCaseInsensitive_ReturnsMatchingProducts()
    {
        using var factory = new PostgreSqlWebApplicationFactory();

        // Apply migrations to the test database.
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var migrations = db.Database.GetMigrations();

            Assert.Contains(
                migrations,
                migration => migration.EndsWith("_InitialCreate"));

            await db.Database.MigrateAsync();

            db.Products.RemoveRange(await db.Products.ToListAsync());
            await db.SaveChangesAsync();
        }

        using var client = factory.CreateClient();

        var keyboard = new CreateProductDto
        {
            Name = "Mechanical Keyboard",
            Description = "Test product",
            Price = 50,
            Quantity = 10
        };

        var mouse = new CreateProductDto
        {
            Name = "Wireless Mouse",
            Description = "Test product",
            Price = 25,
            Quantity = 5
        };

        var keyboardResponse =
            await client.PostAsJsonAsync("/api/products", keyboard);

        var mouseResponse =
            await client.PostAsJsonAsync("/api/products", mouse);

        Assert.Equal(HttpStatusCode.Created, keyboardResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Created, mouseResponse.StatusCode);

        var result = await client.GetFromJsonAsync<PagedResult<ProductDto>>(
            "/api/products?search=KEYBOARD");

        Assert.NotNull(result);
        var product = Assert.Single(result.Items);
        Assert.Equal("Mechanical Keyboard", product.Name);
        Assert.Equal(1, result.TotalItems);
    }
}