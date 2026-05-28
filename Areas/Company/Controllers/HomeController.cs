using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace StoreManagementSystem.Areas.Company.Controllers
{
    [Area("Company")]
    [Authorize(Roles = "Admin")] // This ensures the dashboard requires the Entra ID login
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}