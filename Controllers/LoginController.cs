using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Linq;
using DbOptions.Models; // Убедись, что у тебя есть подходящий контекст базы данных
using Microsoft.EntityFrameworkCore;
using DbOptions; // Если ты используешь EF Core
using Microsoft.AspNetCore.Identity;

namespace OnlineShopingProject.Controllers
{
    public class LoginController : Controller
    {
        private readonly DbContextShop _context; // Контекст базы данных

        public LoginController(DbContextShop context)
        {
            _context = context;
        }

        // GET: /Login
        public IActionResult LoginPage()
        {
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToAction("UserInfo", "LoginUserInfo");
            }

            return View();
        }

        // POST: /Login
        [HttpPost]
        public async Task<IActionResult> Login(string username, string password)
        {
            var user = await _context.Users
                .Where(u => u.Username == username)
                .FirstOrDefaultAsync();

            if (user != null && VerifyPassword(user.passwordHash, password))
            {
                // Логирование идентификатора пользователя
                Console.WriteLine($"-------------------------------------------------User ID: {user.Id}");

                var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                        new Claim(ClaimTypes.Name, user.Username),
                        new Claim(ClaimTypes.Email, user.Email),
                    };

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var authProperties = new AuthenticationProperties
                {
                    // Дополнительные параметры
                };

                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity), authProperties);

                return RedirectToAction("IndexLoggedAccount", "Home");
            }

            ModelState.AddModelError(string.Empty, "Неверное имя пользователя или пароль.");
            return View("LoginPage");
        }


        // Метод для проверки пароля
        private bool VerifyPassword(string hashedPassword, string password)
        {
            var hasher = new PasswordHasher<user_entity>();
            var result = hasher.VerifyHashedPassword(new user_entity(), hashedPassword, password);

            return result == PasswordVerificationResult.Success;
        }

        // POST: /Logout
        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("LoginPage");
        }
    }
}
