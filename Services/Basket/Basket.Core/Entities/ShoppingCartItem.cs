using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Basket.Core.Entities;

public class ShoppingCartItem
{
    [Key]
    public int Id { get; set; }
    public int ShoppingCartId { get; set; }
    public int Quantity { get; set; }
    public decimal Price { get; set; }
    public string ProductId { get; set; } = string.Empty;
    public string ImageFile { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
}