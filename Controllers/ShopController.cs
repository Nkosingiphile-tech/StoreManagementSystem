using Microsoft.AspNetCore.Mvc;
using StoreManagementSystem.Models;
using System.Linq;

namespace StoreManagementSystem.Controllers
{
    public class ShopController : Controller
    {
        private readonly StoreManagementDbContext _context;

        // Inject your existing database context
        public ShopController(StoreManagementDbContext context)
        {
            _context = context;
        }

        // GET: /Shop/
        // This acts as the main storefront catalog
        public IActionResult Index()
        {
            var products = _context.Products.ToList();
            return View(products);
        }

        // GET: /Shop/Details/5
        // Shows a single product before adding it to the cart
        public IActionResult Details(int id)
        {
            var product = _context.Products.FirstOrDefault(p => p.ProductId == id);

            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }
    }
}