using Basket.Core.Entities;
using Basket.Core.Repositories;
using Basket.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Basket.Infrastructure.Repositories;

public class BasketRepository : IBasketRepository
{
    private readonly BasketContext _context;

    public BasketRepository(BasketContext context)
    {
        _context = context;
    }

    public async Task<ShoppingCart> GetBasket(string userName)
    {
        return (await _context.ShoppingCarts
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.UserName == userName))!;
    }

    public async Task<ShoppingCart> UpdateBasket(ShoppingCart shoppingCart)
    {
        var existing = await _context.ShoppingCarts
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.UserName == shoppingCart.UserName);

        if (existing == null)
        {
            _context.ShoppingCarts.Add(shoppingCart);
        }
        else
        {
            // Remove old items and replace with new ones
            _context.ShoppingCartItems.RemoveRange(existing.Items);
            existing.Items = shoppingCart.Items;
            _context.ShoppingCarts.Update(existing);
        }

        await _context.SaveChangesAsync();
        return await GetBasket(shoppingCart.UserName);
    }

    public async Task DeleteBasket(string userName)
    {
        var cart = await _context.ShoppingCarts
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.UserName == userName);

        if (cart != null)
        {
            _context.ShoppingCarts.Remove(cart);
            await _context.SaveChangesAsync();
        }
    }
}