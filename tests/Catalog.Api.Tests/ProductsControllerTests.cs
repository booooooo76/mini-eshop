using System.Net;
using System.Net.Http.Json;
using Catalog.Api.Models;
using Xunit;

namespace Catalog.Api.Tests;

public class ProductsControllerTests(CatalogWebApplicationFactory factory) : IClassFixture<CatalogWebApplicationFactory>
{
    [Fact]
    public async Task GetAll_ReturnsSeededProducts()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/products");

        response.EnsureSuccessStatusCode();
        var products = await response.Content.ReadFromJsonAsync<List<Product>>();
        Assert.NotNull(products);
        Assert.NotEmpty(products!);
    }

    [Fact]
    public async Task GetById_UnknownId_ReturnsNotFound()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync($"/api/products/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Create_ThenGetById_ReturnsCreatedProduct()
    {
        var client = factory.CreateClient();
        var newProduct = new Product { Name = "Test Product", Description = "Integration test", Price = 9.99m, Stock = 1 };

        var createResponse = await client.PostAsJsonAsync("/api/products", newProduct);
        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<Product>();

        var getResponse = await client.GetAsync($"/api/products/{created!.Id}");
        getResponse.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task Create_WithNegativePrice_ReturnsBadRequest()
    {
        var client = factory.CreateClient();
        var invalidProduct = new Product { Name = "Bad", Price = -5m, Stock = 1 };

        var response = await client.PostAsJsonAsync("/api/products", invalidProduct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
