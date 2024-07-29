using DbOptions;
using Microsoft.AspNetCore.Mvc;
using OnlineShopingProject.Models;
using System.Diagnostics;
using Microsoft.EntityFrameworkCore;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly DbContextShop _context;

    public HomeController(DbContextShop context, ILogger<HomeController> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        return View(await _context.Categories.ToListAsync());
    }

    public async Task<IActionResult> IndexLoggedAccount()
    {
        // Предполагается, что у вас есть DbSet<category_entity> в вашем контексте
        var categories = await _context.Categories.ToListAsync();
        return View(categories);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
