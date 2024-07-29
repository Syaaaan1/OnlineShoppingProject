using DbOptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace OnlineShopingProject.Controllers
{
    public class SelectedCategoryController : Controller
    {
        private readonly DbContextShop _context;

        public SelectedCategoryController(DbContextShop context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(int categoryId)
        {
            var products = await _context.Products
                .Where(p => p.CategoryId == categoryId)
                .ToListAsync();

            return View(products);
        }

        public async Task<IActionResult> AllItems()
        {
            var products = await _context.Products.ToListAsync();
            return View(products); // Обязательно создайте соответствующее представление для всех товаров
        }
    }
}