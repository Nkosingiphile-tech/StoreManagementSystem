using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using StoreManagementSystem.Models;

namespace StoreManagementSystem.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;

    public HomeController(ILogger<HomeController> logger)
    {
        _logger = logger;
    }

    public IActionResult Index()
    {
        // If the user logging in has the Admin role, route them instantly to the dashboard
        if (User.IsInRole("Admin"))
        {
            return RedirectToAction("Index", "Home", new { area = "Company" });
        }
        return View();
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
