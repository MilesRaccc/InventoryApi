using System.Text;
using InventoryApi.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;

namespace InventoryApi.Tests;

public class PostgreSqlWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddUserSecrets<PostgreSqlWebApplicationFactory>(
                optional: true);
        });

        builder.ConfigureServices((context, services) =>
        {
            var connectionString =
                context.Configuration.GetConnectionString("PostgreSqlTest")
                ?? Environment.GetEnvironmentVariable("INVENTORYAPI_TEST_CONNECTION");

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException(
                    "Configure the PostgreSqlTest connection string in User Secrets.");
            }

            services.RemoveAll<AppDbContext>();
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<AppDbContext>>();

            services.AddDbContext<AppDbContext>(options =>
                options.UseNpgsql(connectionString));
            services.PostConfigure<JwtBearerOptions>(
                JwtBearerDefaults.AuthenticationScheme,
                options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidIssuer = "InventoryApi",

                        ValidateAudience = true,
                        ValidAudience = "InventoryApi.Client",

                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(
                            Encoding.UTF8.GetBytes(
                                "xKDagvH5tpDwN370ckZN0yYG8yC4QYN56rTKKf7IIH4=")),

                        ValidateLifetime = true,
                        ClockSkew = TimeSpan.Zero
                    };
                });
        });
    }

    public HttpClient CreateAuthenticatedClient()
    {
        var client = CreateClient();

        var token = JwtTestTokenFactory.CreateToken();

        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue(
                "Bearer", token);

        return client;
    }
}
