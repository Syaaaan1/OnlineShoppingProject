using DbOptions;
using DbOptions.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace OnlineShopingProject.Controllers
{
    public class SearchProductController : Controller
    {
        private readonly DbContextShop _context;

        public SearchProductController(DbContextShop context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Search(string searchTerm)
        {
            if (string.IsNullOrEmpty(searchTerm))
            {
                return View(new List<product_entity>()); // Пустой список, если нет поискового термина
            }

            var results = await _context.Products
                .Where(p => p.Name.Contains(searchTerm) || p.Description.Contains(searchTerm))
                .ToListAsync();

            return View(results);
        }
    }
}
