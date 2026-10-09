using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using InventoryApi.Data;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace InventoryApi.Tests;

public class ProductsApiTests
{

    #region Get Product Tests

    [Fact]
    public async Task GetProducts_ReturnsOk()
    {
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Database.EnsureCreated();
        }

        var response = await client.GetAsync("/api/products");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetProduct_WithNonexistentId_ReturnsNotFound()
    {
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Database.EnsureCreated();
        }

        var response = await client.GetAsync("/api/products/999999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    #region Paging Tests
    [Fact]
    public async Task GetProducts_WithPageSize_ReturnsRequestedNumberOfProducts()
    {
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Database.EnsureCreated();
        }

        for (var i = 1; i <= 3; i++)
        {
            var product = new
            {
                Name = $"Product {i}",
                Description = $"Description {i}",
                Price = 10m * i,
                Quantity = i
            };

            var response = await client.PostAsJsonAsync(
                "/api/products", product);

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }

        var getResponse = await client.GetAsync(
            "/api/products?page=1&pageSize=2");

        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var result = await getResponse.Content
            .ReadFromJsonAsync<JsonElement>();

        Assert.Equal(2, result.GetProperty("items").GetArrayLength());
        Assert.Equal(3, result.GetProperty("totalItems").GetInt32());
        Assert.Equal(1, result.GetProperty("page").GetInt32());
        Assert.Equal(2, result.GetProperty("pageSize").GetInt32());
        Assert.Equal(2, result.GetProperty("totalPages").GetInt32());
    }

    [Fact]
    public async Task GetProducts_SecondPage_ReturnsRemainingProduct()
    {
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Database.EnsureCreated();
        }

        for (var i = 1; i <= 3; i++)
        {
            var response = await client.PostAsJsonAsync(
                "/api/products",
                new
                {
                    Name = $"Product {i}",
                    Description = $"Description {i}",
                    Price = 10m * i,
                    Quantity = i
                });

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }

        var responsePage = await client.GetAsync(
            "/api/products?page=2&pageSize=2");

        Assert.Equal(HttpStatusCode.OK, responsePage.StatusCode);

        var result = await responsePage.Content
            .ReadFromJsonAsync<JsonElement>();

        Assert.Equal(1, result.GetProperty("items").GetArrayLength());
        Assert.Equal(3, result.GetProperty("totalItems").GetInt32());
        Assert.Equal(2, result.GetProperty("page").GetInt32());
        Assert.Equal(2, result.GetProperty("pageSize").GetInt32());
        Assert.Equal(2, result.GetProperty("totalPages").GetInt32());
    }

    [Fact]
    public async Task GetProducts_WithPageSizeAboveLimit_ReturnsBadRequest()
    {
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Database.EnsureCreated();
        }

        var response = await client.GetAsync(
            "/api/products?pageSize=101");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    #endregion

    #region Sorting Tests

    [Fact]
    public async Task GetProducts_SortByPriceAscending_ReturnsProductsInOrder()
    {
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Database.EnsureCreated();
        }

        foreach (var quantity in new[] { 3, 1, 2 })
        {
            var response = await client.PostAsJsonAsync(
                "/api/products",
                new
                {
                    Name = $"Product {quantity}",
                    Description = "Test product",
                    Price = 10m,
                    Quantity = quantity
                });

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }

        var responseGet = await client.GetAsync(
            "/api/products?sortBy=quantity&sortOrder=asc");

        Assert.Equal(HttpStatusCode.OK, responseGet.StatusCode);

        var result = await responseGet.Content
            .ReadFromJsonAsync<JsonElement>();

        var items = result.GetProperty("items")
            .EnumerateArray()
            .Select(item => item.GetProperty("quantity").GetInt32())
            .ToArray();

        Assert.Equal(new[] { 1, 2, 3 }, items);
    }

    [Fact]
    public async Task GetProducts_SortByQuantityDescending_ReturnsProductsInOrder()
    {
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Database.EnsureCreated();
        }

        foreach (var quantity in new[] { 1, 3, 2 })
        {
            var response = await client.PostAsJsonAsync(
                "/api/products",
                new
                {
                    Name = $"Product {quantity}",
                    Description = "Test product",
                    Price = 10m,
                    Quantity = quantity
                });

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }

        var responseGet = await client.GetAsync(
            "/api/products?sortBy=quantity&sortOrder=desc");

        Assert.Equal(HttpStatusCode.OK, responseGet.StatusCode);

        var result = await responseGet.Content
            .ReadFromJsonAsync<JsonElement>();

        var quantities = result.GetProperty("items")
            .EnumerateArray()
            .Select(item => item.GetProperty("quantity").GetInt32())
            .ToArray();

        Assert.Equal(new[] { 3, 2, 1 }, quantities);
    }

    #endregion

    #region Filtration Tests

    [Fact]
    public async Task GetProducts_WithPriceRange_ReturnsMatchingProducts()
    {
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Database.EnsureCreated();
        }

        foreach (var price in new[] { 10m, 30m, 50m })
        {
            var response = await client.PostAsJsonAsync(
                "/api/products",
                new
                {
                    Name = $"Product {price}",
                    Description = "Test product",
                    Price = price,
                    Quantity = 1
                });

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }

        var responseGet = await client.GetAsync(
            "/api/products?minPrice=20&maxPrice=40");

        Assert.Equal(HttpStatusCode.OK, responseGet.StatusCode);

        var result = await responseGet.Content
            .ReadFromJsonAsync<JsonElement>();

        var items = result.GetProperty("items").EnumerateArray().ToArray();

        Assert.Single(items);
        Assert.Equal(30m, items[0].GetProperty("price").GetDecimal());
        Assert.Equal(1, result.GetProperty("totalItems").GetInt32());
    }

    [Fact]
    public async Task GetProducts_WithMinQuantity_ReturnsMatchingProducts()
    {
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Database.EnsureCreated();
        }

        foreach (var quantity in new[] { 0, 5, 10 })
        {
            var response = await client.PostAsJsonAsync(
                "/api/products",
                new
                {
                    Name = $"Product {quantity}",
                    Description = "Test product",
                    Price = 20m,
                    Quantity = quantity
                });

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }

        var responseGet = await client.GetAsync(
            "/api/products?minQuantity=5");

        Assert.Equal(HttpStatusCode.OK, responseGet.StatusCode);

        var result = await responseGet.Content
            .ReadFromJsonAsync<JsonElement>();

        var items = result.GetProperty("items").EnumerateArray().ToArray();

        Assert.Equal(2, items.Length);
        Assert.All(items, item =>
            Assert.True(item.GetProperty("quantity").GetInt32() >= 5));

        Assert.Equal(2, result.GetProperty("totalItems").GetInt32());
    }

    [Fact]
    public async Task GetProducts_WithMinPriceGreaterThanMaxPrice_ReturnsBadRequest()
    {
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Database.EnsureCreated();
        }

        var response = await client.GetAsync(
            "/api/products?minPrice=50&maxPrice=20");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    #endregion

    #endregion

    #region Create Product Tests

    [Fact]
    public async Task CreateProduct_WithValidData_ReturnsCreated()
    {
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Database.EnsureCreated();
        }

        var product = new
        {
            Name = "Test keyboard",
            Description = "Mechanical keyboard",
            Price = 79.99m,
            Quantity = 5
        };

        var response = await client.PostAsJsonAsync(
            "/api/products", product);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task CreateProduct_WithNegativePrice_ReturnsBadRequest()
    {
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Database.EnsureCreated();
        }

        var product = new
        {
            Name = "Invalid product",
            Description = "Price cannot be negative",
            Price = -10m,
            Quantity = 5
        };

        var response = await client.PostAsJsonAsync(
            "/api/products", product);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateProduct_ReturnsCreatedProduct()
    {
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Database.EnsureCreated();
        }

        var product = new
        {
            Name = "Test mouse",
            Description = "Wireless mouse",
            Price = 29.99m,
            Quantity = 3
        };

        var response = await client.PostAsJsonAsync(
            "/api/products", product);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var createdProduct = await response.Content
            .ReadFromJsonAsync<JsonElement>();

        Assert.True(createdProduct.TryGetProperty("id", out var id));
        Assert.True(id.GetInt32() > 0);

        Assert.Equal("Test mouse",
            createdProduct.GetProperty("name").GetString());

        Assert.Equal(29.99m,
            createdProduct.GetProperty("price").GetDecimal());

        Assert.Equal(3,
            createdProduct.GetProperty("quantity").GetInt32());
    }

    [Fact]
    public async Task CreateProduct_ThenGetById_ReturnsSameProduct()
    {
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Database.EnsureCreated();
        }

        var product = new
        {
            Name = "Test monitor",
            Description = "27-inch monitor",
            Price = 199.99m,
            Quantity = 2
        };

        var createResponse = await client.PostAsJsonAsync(
            "/api/products", product);

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var createdProduct = await createResponse.Content
            .ReadFromJsonAsync<JsonElement>();

        var id = createdProduct.GetProperty("id").GetInt32();

        var getResponse = await client.GetAsync($"/api/products/{id}");

        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var retrievedProduct = await getResponse.Content
            .ReadFromJsonAsync<JsonElement>();

        Assert.Equal(id, retrievedProduct.GetProperty("id").GetInt32());
        Assert.Equal("Test monitor",
            retrievedProduct.GetProperty("name").GetString());
        Assert.Equal(199.99m,
            retrievedProduct.GetProperty("price").GetDecimal());
        Assert.Equal(2,
            retrievedProduct.GetProperty("quantity").GetInt32());
    }

    #endregion

    #region Update Product Tests

    [Fact]
    public async Task UpdateProduct_WithValidData_UpdatesProduct()
    {
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Database.EnsureCreated();
        }

        var product = new
        {
            Name = "Old keyboard",
            Description = "Old description",
            Price = 50m,
            Quantity = 4
        };

        var createResponse = await client.PostAsJsonAsync(
            "/api/products", product);

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var createdProduct = await createResponse.Content
            .ReadFromJsonAsync<JsonElement>();

        var id = createdProduct.GetProperty("id").GetInt32();

        var updatedProduct = new
        {
            Name = "New keyboard",
            Description = "Updated description",
            Price = 75.50m,
            Quantity = 8
        };

        var updateResponse = await client.PutAsJsonAsync(
            $"/api/products/{id}", updatedProduct);

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        var getResponse = await client.GetAsync($"/api/products/{id}");

        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var result = await getResponse.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal("New keyboard",
            result.GetProperty("name").GetString());
        Assert.Equal("Updated description",
            result.GetProperty("description").GetString());
        Assert.Equal(75.50m,
            result.GetProperty("price").GetDecimal());
        Assert.Equal(8,
            result.GetProperty("quantity").GetInt32());
    }


    [Fact]
    public async Task UpdateProduct_WithNonexistentId_ReturnsNotFound()
    {
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Database.EnsureCreated();
        }

        var updatedProduct = new
        {
            Name = "Updated keyboard",
            Description = "Updated description",
            Price = 75.50m,
            Quantity = 8
        };

        var response = await client.PutAsJsonAsync(
            "/api/products/999999", updatedProduct);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    #endregion

    #region Delete Product Tests

    [Fact]
    public async Task DeleteProduct_RemovesProduct()
    {
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Database.EnsureCreated();
        }

        var product = new
        {
            Name = "Temporary keyboard",
            Description = "Product to delete",
            Price = 40m,
            Quantity = 3
        };

        var createResponse = await client.PostAsJsonAsync(
            "/api/products", product);

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var createdProduct = await createResponse.Content
            .ReadFromJsonAsync<JsonElement>();

        var id = createdProduct.GetProperty("id").GetInt32();

        var deleteResponse = await client.DeleteAsync(
            $"/api/products/{id}");

        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var getResponse = await client.GetAsync(
            $"/api/products/{id}");

        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task DeleteProduct_WithNonexistentId_ReturnsNotFound()
    {
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Database.EnsureCreated();
        }

        var response = await client.DeleteAsync("/api/products/999999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    #endregion
}