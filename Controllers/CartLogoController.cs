using Microsoft.AspNetCore.Mvc;
using DbOptions.Models;
using Microsoft.EntityFrameworkCore;
using DbOptions;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System.Threading.Tasks;

namespace OnlineShopingProject.Controllers
{
    [Route("Cart")]
    public class CartLogoController : Controller
    {
        private readonly DbContextShop _context;
        private const string CartSessionKey = "CartId";

        public CartLogoController(DbContextShop context)
        {
            _context = context;
        }

        // Метод для отображения страницы корзины
        public async Task<IActionResult> CartView()
        {
            try
            {
                Cart cart;
                if (User.Identity.IsAuthenticated)
                {
                    var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                    Console.WriteLine($"User ID: {userId}"); // Добавьте логирование
                    if (string.IsNullOrEmpty(userId))
                    {
                        Console.WriteLine("Authenticated user, but userId is null or empty");
                        return RedirectToAction("Error", "Home", new { message = "User identification failed" });
                    }
                    cart = await GetOrCreateCartForUser(userId);
                }
                else
                {
                    cart = await GetOrCreateCartForSession();
                }

                return View(cart);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in CartView: {ex.Message}");
                return RedirectToAction("Error", "Home", new { message = "An error occurred while retrieving the cart" });
            }
        }


        [HttpPost]
        [Route("AddToCart")]
        public async Task<IActionResult> AddToCart(int productId, int quantity)
        {
            var product = await _context.Products.FindAsync(productId);
            if (product == null)
                return NotFound();

            Cart cart;
            if (User.Identity.IsAuthenticated)
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    // Обработка ошибки: не удалось получить идентификатор пользователя
                    return RedirectToAction("Error", "Home");
                }
                cart = await GetOrCreateCartForUser(userId);
            }
            else
            {
                cart = await GetOrCreateCartForSession();
            }

            var cartItem = cart.Items.FirstOrDefault(i => i.ProductId == productId);
            if (cartItem == null)
            {
                cartItem = new CartItem
                {
                    ProductId = productId,
                    Quantity = quantity,
                    Price = product.Price
                };
                cart.Items.Add(cartItem);
            }
            else
            {
                cartItem.Quantity += quantity;
            }

            await _context.SaveChangesAsync();

            return Redirect(Request.Headers["Referer"].ToString());
        }

        private async Task<Cart> GetOrCreateCartForUser(string userId)
        {
            var cart = await _context.Carts
                .Include(c => c.Items)
                .ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart == null)
            {
                cart = new Cart { UserId = userId };
                _context.Carts.Add(cart);
                await _context.SaveChangesAsync();
            }

            return cart;
        }

        private async Task<Cart> GetOrCreateCartForSession()
        {
            string cartId = HttpContext.Session.GetString(CartSessionKey);
            if (string.IsNullOrEmpty(cartId))
            {
                cartId = Guid.NewGuid().ToString();
                HttpContext.Session.SetString(CartSessionKey, cartId);
            }

            var cart = await _context.Carts
                .Include(c => c.Items)
                .ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(c => c.UserId == cartId);

            if (cart == null)
            {
                cart = new Cart { UserId = cartId };
                _context.Carts.Add(cart);
                await _context.SaveChangesAsync();
            }

            return cart;
        }

        [HttpPost]
        [Route("UpdateQuantity")]
        public async Task<IActionResult> UpdateQuantity(int itemId, int newQuantity)
        {
            var cartItem = await _context.CartItems.FindAsync(itemId);
            if (cartItem == null)
                return NotFound();

            cartItem.Quantity = newQuantity;
            await _context.SaveChangesAsync();

            var cart = await _context.Carts
                .Include(c => c.Items)
                .FirstOrDefaultAsync(c => c.Id == cartItem.CartId);

            return RedirectToAction("CartView");
        }

        [HttpPost]
        [Route("RemoveItem")]
        public async Task<IActionResult> RemoveItem(int itemId)
        {
            var cartItem = await _context.CartItems.FindAsync(itemId);
            if (cartItem == null)
                return NotFound();

            _context.CartItems.Remove(cartItem);
            await _context.SaveChangesAsync();

            var cart = await _context.Carts
                .Include(c => c.Items)
                .FirstOrDefaultAsync(c => c.Id == cartItem.CartId);

            return RedirectToAction("CartView");
        }
    }
}
