using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StoreManagementSystem.Models;

namespace StoreManagementSystem.Areas.Company.Controllers
{
    // The Controller handles all user requests related to Categories and manages data flow to the Views.
    [Area("Company")] // This attribute specifies that this controller belongs to the "Company" area of the application
    [Authorize] // This attribute restricts access to authenticated users only, ensuring that only logged-in users can manage categories
    public class CategoriesController : Controller
    {
        // Private field for the Entity Framework database context
        private readonly StoreManagementDbContext _context;

        // Constructor: Injects the database context when the controller is created
        public CategoriesController(StoreManagementDbContext context)
        {
            _context = context;
        }

        // --- READ: Get all categories ---
        // GET: Categories
        // This method fetches the list of categories from the database and passes it to the Index view.
        public async Task<IActionResult> Index()
        {
            // LINQ: Execute an asynchronous query to get all categories as a list
            return View(await _context.Categories.ToListAsync());
        }

        // --- READ: Get a single category ---
        // GET: Categories/Details/5
        // This method finds a specific category by its ID to display its details.
        public async Task<IActionResult> Details(int? id)
        {
            // If the ID parameter is missing, return a 404 error
            if (id == null)
            {
                return NotFound();
            }

            // LINQ: Find the first category that matches the provided ID
            var category = await _context.Categories
                .FirstOrDefaultAsync(m => m.CategoryId == id);
            
            // If the category does not exist in the database, return a 404 error
            if (category == null)
            {
                return NotFound();
            }

            // Return the Details view with the category data
            return View(category);
        }

        // --- CREATE: Show the form ---
        // GET: Categories/Create
        // This method displays the empty form to create a new category.
        public IActionResult Create()
        {
            return View();
        }

        // --- CREATE: Save the form data ---
        // POST: Categories/Create
        // This method receives the form submission and saves the new category to the database.
        [HttpPost]
        [ValidateAntiForgeryToken] // Security token to prevent Cross-Site Request Forgery
        public async Task<IActionResult> Create([Bind("CategoryId,CategoryName,Description,IsActive")] Category category)
        {
            // Check if the submitted data passes model validation rules
            if (ModelState.IsValid)
            {
                // Add the new category to the context
                _context.Add(category);
                // Save changes asynchronously to SQL Server
                await _context.SaveChangesAsync();
                // Redirect back to the main category list
                return RedirectToAction(nameof(Index));
            }
            
            // If validation fails, return the form with errors
            return View(category);
        }

        // --- UPDATE: Show the form ---
        // GET: Categories/Edit/5
        // This method fetches a category and populates the edit form.
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            // Find the category by primary key
            var category = await _context.Categories.FindAsync(id);
            if (category == null)
            {
                return NotFound();
            }
            return View(category);
        }

        // --- UPDATE: Save the form data ---
        // POST: Categories/Edit/5
        // This method receives the updated category data and saves it to the database.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("CategoryId,CategoryName,Description,IsActive")] Category category)
        {
            // Ensure URL ID matches the submitted object ID
            if (id != category.CategoryId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Update the category in the tracking context
                    _context.Update(category);
                    // Save changes to the database
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CategoryExists(category.CategoryId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(category);
        }

        // --- DELETE: Show confirmation screen ---
        // GET: Categories/Delete/5
        // This method displays a confirmation page asking if the user is sure they want to delete.
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            // LINQ: Fetch the category for the confirmation screen
            var category = await _context.Categories
                .FirstOrDefaultAsync(m => m.CategoryId == id);
            
            if (category == null)
            {
                return NotFound();
            }

            return View(category);
        }

        // --- DELETE: Execute the deletion ---
        // POST: Categories/Delete/5
        // This method permanently removes the category from the database.
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category != null)
            {
                _context.Categories.Remove(category);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // Helper Method: Checks if a category exists to handle concurrent edit conflicts
        private bool CategoryExists(int id)
        {
            // LINQ: Returns true if any category matches the ID
            return _context.Categories.Any(e => e.CategoryId == id);
        }
    }
}