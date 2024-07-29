using DbOptions.Models;
using DbOptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

public class OrderController : Controller
{
    private readonly DbContextShop _context;

    public OrderController(DbContextShop context)
    {
        _context = context;
    }

    [Authorize]
    public async Task<IActionResult> Checkout()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var cart = await _context.Carts
            .Include(c => c.Items)
            .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(c => c.UserId == userId);

        if (cart == null || !cart.Items.Any())
        {
            return RedirectToAction("CartView", "CartLogo");
        }

        var order = new order_entity
        {
            UserId = Guid.Parse(userId),
            OrderDate = DateTime.Now,
            TotalAmount = cart.TotalPrice,
            // Добавьте другие необходимые поля
        };

        _context.Orders.Add(order);
        _context.CartItems.RemoveRange(cart.Items);
        await _context.SaveChangesAsync();

        return RedirectToAction("OrderConfirmation", new { orderId = order.Id });
    }

    [Authorize]
    public async Task<IActionResult> OrderConfirmation(int orderId)
    {
        var order = await _context.Orders.FindAsync(orderId);
        if (order == null)
        {
            return NotFound();
        }

        return View(order);
    }

    [Authorize]
    public async Task<IActionResult> OrderHistory()
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
        var orders = await _context.Orders
            .Where(o => o.UserId == userId)
            .OrderByDescending(o => o.OrderDate)
            .ToListAsync();

        return View(orders);
    }
}