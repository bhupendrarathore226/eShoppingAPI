namespace Catalog.Core.Entities;

public class Product : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ImageFile { get; set; } = string.Empty;
    public string? BrandId { get; set; }
    public ProductBrand? Brands { get; set; }
    public string? TypeId { get; set; }
    public ProductType? Types { get; set; }
    public decimal Price { get; set; }
}