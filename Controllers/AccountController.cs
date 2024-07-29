using Microsoft.AspNetCore.Mvc;
using DbOptions.Models;
using System.Linq;
using HashService.Services;
using DbOptions;

namespace YourNamespace.Controllers
{
    public class AccountController : Controller
    {
        private readonly DbContextShop _context;
        private readonly PasswordHasherService _passwordHasher;

        public AccountController(DbContextShop context)
        {
            _context = context;
            _passwordHasher = new PasswordHasherService();
        }

        // Отображает форму для ввода номера телефона
        [HttpGet]
        public IActionResult SignUp()
        {
            return View();
        }

        // Обрабатывает ввод номера телефона
        [HttpPost]
        public IActionResult SignUp(string phoneNumber)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber))
            {
                ModelState.AddModelError("PhoneNumber", "Phone number is required.");
                return View();
            }

            // Проверяем, существует ли пользователь с таким номером телефона
            var existingUser = _context.Users
                .FirstOrDefault(u => u.phoneNumber == phoneNumber);

            if (existingUser != null)
            {
                // Пользователь уже существует
                return RedirectToAction("UserExists");
            }

            // Пользователь не найден, переходим к следующему шагу
            return RedirectToAction("SignUpStepTwo", new { phoneNumber });
        }

        // Отображает форму для ввода остальных данных
        [HttpGet]
        public IActionResult SignUpStepTwo(string phoneNumber)
        {
            ViewBag.PhoneNumber = phoneNumber;
            return View();
        }
        
        // Обрабатывает ввод данных и создаёт нового пользователя
        [HttpPost]
        public IActionResult CreateUser(user_entity model)
        {
            Console.WriteLine("CreateUser method called.");

            if (ModelState.IsValid)
            {
                Console.WriteLine("Model is valid.");

                model.Id = Guid.NewGuid();
                model.passwordHash = _passwordHasher.HashPassword(model.passwordHash); // Хеширование пароля

                _context.Users.Add(model);
                _context.SaveChanges();

                Console.WriteLine("User successfully created.");

                // Успешная регистрация
                return RedirectToAction("RegistrationSuccess");
            }

            Console.WriteLine("Model is invalid.");

            // Вывод ошибок валидации
            foreach (var state in ModelState)
            {
                foreach (var error in state.Value.Errors)
                {
                    Console.WriteLine($"Error in {state.Key}: {error.ErrorMessage}");
                }
            }

            // Возврат формы с ошибками
            return View("SignUpStepTwo", model);
        }


        // Страница уведомления о существующем пользователе
        public IActionResult UserExists()
        {
            return View();
        }

        // Страница успешной регистрации
        public IActionResult RegistrationSuccess()
        {
            return View();
        }
    }
}
