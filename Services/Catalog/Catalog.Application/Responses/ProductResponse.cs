using Catalog.Core.Entities;

namespace Catalog.Application.Responses;

public class ProductResponse
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ImageFile { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public ProductBrand? Brands { get; set; }
    public ProductType? Types { get; set; }
}