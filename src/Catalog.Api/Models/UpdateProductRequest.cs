namespace Catalog.Api.Models;

public record UpdateProductRequest(string Name, string Description, decimal Price, int Stock);
