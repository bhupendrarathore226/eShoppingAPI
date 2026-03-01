using System.ComponentModel.DataAnnotations;

namespace Basket.Core.Entities;

public class ShoppingCart
{
    [Key]
    public int Id { get; set; }
    public string UserName { get; set; } = string.Empty;
    public List<ShoppingCartItem> Items { get; set; } = new List<ShoppingCartItem>();
    public ShoppingCart()
    {
        
    }

    public ShoppingCart(string userName)
    {
        UserName = userName;
    }
}