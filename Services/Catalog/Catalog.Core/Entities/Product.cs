namespace Catalog.Core.Entities;

public class Product : BaseEntity
{
    public string Name { get; set; }
    public string Summary { get; set; }
    public string Description { get; set; }
    public string ImageFile { get; set; }
    public string? BrandId { get; set; }
    public ProductBrand? Brands { get; set; }
    public string? TypeId { get; set; }
    public ProductType? Types { get; set; }
    public decimal Price { get; set; }
}