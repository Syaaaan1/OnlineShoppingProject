using DbOptions.Models;
using DbOptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Net.Mail;
using System.Net;

public class OrderController : Controller
{
    private readonly DbContextShop _context;
    private const string CartSessionKey = "CartId";
    private readonly IConfiguration _configuration;


    public OrderController(DbContextShop context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    /*[Authorize]
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
            OrderDate = DateTime.UtcNow,
            TotalAmount = cart.TotalPrice,
            Status = "Active", // Устанавливаем статус по умолчанию
            Products = cart.Items.Select(i => i.Product).ToList(),
            DeliveryAddress = "Default Address", // Установите значение по умолчанию
            PaymentMethod = "Default Payment", // Установите значение по умолчанию
        };

        _context.Orders.Add(order);
        _context.CartItems.RemoveRange(cart.Items);
        await _context.SaveChangesAsync();

        return RedirectToAction("OrderConfirmation", new { orderId = order.Id });
    }*/

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

    [HttpGet]
    public IActionResult FastCheckout()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> FastCheckout(string firstName, string lastName, string middleName, string phoneNumber, string deliveryAddress, string paymentMethod, string comment)
    {


        var cart = await GetOrCreateCartForSession();

        if (cart == null || !cart.Items.Any())
        {
            return RedirectToAction("CartView", "CartLogo");
        }

        var order = new order_entity
        {
            FirstName = firstName,
            LastName = lastName,
            MiddleName = middleName,
            PhoneNumber = phoneNumber,
            OrderDate = DateTime.UtcNow,
            TotalAmount = cart.TotalPrice,
            DeliveryAddress = deliveryAddress,
            PaymentMethod = paymentMethod,
            Comment = comment,
            Products = cart.Items.Select(i => i.Product).ToList(),
            UserId = Guid.Empty
        };

        var tempUser = new user_entity
        {
            Id = Guid.NewGuid(),
            Username = $"{firstName} {lastName}",
            Email = "",
            phoneNumber = phoneNumber,
            passwordHash = ""
        };

        _context.Users.Add(tempUser);
        order.User = tempUser;
        _context.Orders.Add(order);

        await _context.SaveChangesAsync();
        await SendOrderEmailAsync(firstName, lastName, middleName, phoneNumber, deliveryAddress, paymentMethod, comment, cart);// Отправка электронной почты

        return RedirectToAction("UnauthorizedOrderConfirmation", new { orderId = order.Id });
    }

    private async Task SendOrderEmailAsync(string firstName, string lastName, string middleName, string phoneNumber, string deliveryAddress, string paymentMethod, string comment, Cart cart)
    {
        var smtpServer = _configuration["EmailSettings:SmtpServer"];
        var smtpPortStr = _configuration["EmailSettings:SmtpPort"];
        var smtpUsername = _configuration["EmailSettings:SmtpUsername"];
        var smtpPassword = _configuration["EmailSettings:SmtpPassword"];

        if (string.IsNullOrEmpty(smtpServer) || string.IsNullOrEmpty(smtpPortStr) ||
            string.IsNullOrEmpty(smtpUsername) || string.IsNullOrEmpty(smtpPassword))
        {
            throw new InvalidOperationException("SMTP configuration is missing. Please check your configuration settings.");
        }

        if (!int.TryParse(smtpPortStr, out var smtpPort))
        {
            throw new InvalidOperationException("SMTP port is not a valid number.");
        }

        var smtpClient = new SmtpClient(smtpServer)
        {
            Port = smtpPort,
            Credentials = new NetworkCredential(smtpUsername, smtpPassword),
            EnableSsl = true,
        };

        var mailMessage = new MailMessage
        {
            From = new MailAddress("vokidsstore@gmail.com"),
            Subject = "New Order Received",
            Body = BuildEmailBody(firstName, lastName, middleName, phoneNumber, deliveryAddress, paymentMethod, comment, cart),
            IsBodyHtml = true,
        };

        mailMessage.To.Add("evgeniipechenkin@ukr.net");

        await smtpClient.SendMailAsync(mailMessage);
    }

    private string BuildEmailBody(string firstName, string lastName,string middleName, string phoneNumber, string deliveryAddress, string paymentMethod, string comment, Cart cart)
    {
        var sb = new StringBuilder();
        sb.Append("<h1>Детали нового заказа</h1>");
        sb.Append($"<p><strong>Имя:</strong> {firstName} {lastName} {middleName}</p>");
        sb.Append($"<p><strong>Номер телефона:</strong> {phoneNumber}</p>");
        sb.Append($"<p><strong>Адрес доставки:</strong> {deliveryAddress}</p>");
        sb.Append($"<p><strong>Способ оплаты:</strong> {paymentMethod}</p>");
        sb.Append($"<p><strong>Комментарий:</strong> {comment}</p>");

        sb.Append("<h2>Заказанные продукты</h2>");
        sb.Append("<ul>");

        foreach (var item in cart.Items)
        {
            sb.Append($"<li>{item.Product.Name} - {item.Quantity} шт. x {item.Product.Price:C}</li>");
        }
        sb.Append("</ul>");

        sb.Append($"<p><strong>Общая сумма:</strong> {cart.TotalPrice:C}</p>");

        _context.CartItems.RemoveRange(cart.Items);
        HttpContext.Session.Remove(CartSessionKey);

        return sb.ToString();
    }


    public async Task<IActionResult> UnauthorizedOrderConfirmation(int orderId)
    {
        var order = await _context.Orders
            .Include(o => o.User)
            .Include(o => o.Products)
            .FirstOrDefaultAsync(o => o.Id == orderId);

        if (order == null)
        {
            return NotFound();
        }

        return View(order);
    }
}