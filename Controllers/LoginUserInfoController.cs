/*using DbOptions.Models;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using DbOptions;

namespace OnlineShopingProject.Controllers
{
    public class LoginUserInfoController : Controller
    {
        private readonly DbContextShop _context;


        public LoginUserInfoController(DbContextShop context)
        {
            _context = context;
        }

        public async Task<IActionResult> UserInfo()
        {
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToAction("LoginPage", "Login");
            }

            var username = User.FindFirstValue(ClaimTypes.Name);
            var user = await _context.Users
                .Include(u => u.orders)
                .FirstOrDefaultAsync(u => u.Username == username);

            if (user == null)
            {
                return NotFound();
            }

            return View(user);
        }
    }
}*/