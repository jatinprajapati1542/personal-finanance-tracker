using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using personal_finance_Tracker.Data;
using personal_finance_Tracker.Models;
using personal_finance_Tracker.ViewModels;

namespace personal_finance_Tracker.Controllers;

[Authorize]
public class IncomeController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public IncomeController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }


    // GET: /Income
    // public async Task<IActionResult> Index(
    //     string? search,
    //     int? categoryId,
    //     DateTime? startDate,
    //     DateTime? endDate)
    // {
    //     var userId = _userManager.GetUserId(User);

    //     if (userId == null)
    //     {
    //         return Challenge();
    //     }

    //     var query = _context.Transactions
    //         .Include(t => t.Category)
    //         .Where(t =>
    //             t.UserId == userId &&
    //             t.Type == TransactionType.Income);

    //     // Search
    //     if (!string.IsNullOrWhiteSpace(search))
    //     {
    //         query = query.Where(t =>
    //             (t.Description != null &&
    //              t.Description.Contains(search)) ||
    //             (t.Category != null &&
    //              t.Category.Name.Contains(search)));
    //     }

    //     // Category filter
    //     if (categoryId.HasValue)
    //     {
    //         query = query.Where(t =>
    //             t.CategoryId == categoryId.Value);
    //     }

    //     // Start date
    //     if (startDate.HasValue)
    //     {
    //         query = query.Where(t =>
    //             t.TransactionDate.Date >= startDate.Value.Date);
    //     }

    //     // End date
    //     if (endDate.HasValue)
    //     {
    //         query = query.Where(t =>
    //             t.TransactionDate.Date <= endDate.Value.Date);
    //     }

    //     var incomes = await query
    //         .OrderByDescending(t => t.TransactionDate)
    //         .ThenByDescending(t => t.Id)
    //         .ToListAsync();

    //     var categories = await _context.Categories
    //         .Where(c =>
    //             c.UserId == userId &&
    //             c.Type == TransactionType.Income)
    //         .OrderBy(c => c.Name)
    //         .ToListAsync();

    //     ViewBag.Categories = categories;
    //     ViewBag.Search = search;
    //     ViewBag.CategoryId = categoryId;
    //     ViewBag.StartDate = startDate?.ToString("yyyy-MM-dd");
    //     ViewBag.EndDate = endDate?.ToString("yyyy-MM-dd");

    //     return View(incomes);
    // }
    public async Task<IActionResult> Index(
    string? search,
    int? categoryId,
    DateTime? fromDate,
    DateTime? toDate)
    {
        var userId = _userManager.GetUserId(User);

        if (userId == null)
        {
            return Challenge();
        }

        var query = _context.Transactions
            .Include(t => t.Category)
            .Where(t =>
                t.UserId == userId &&
                t.Type == TransactionType.Income)
            .AsQueryable();

        // Search by description
        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(t =>
                t.Description != null &&
                t.Description.Contains(search));
        }

        // Filter by category
        if (categoryId.HasValue)
        {
            query = query.Where(t =>
                t.CategoryId == categoryId.Value);
        }

        // Filter from date
        if (fromDate.HasValue)
        {
            query = query.Where(t =>
                t.TransactionDate.Date >= fromDate.Value.Date);
        }

        // Filter to date
        if (toDate.HasValue)
        {
            query = query.Where(t =>
                t.TransactionDate.Date <= toDate.Value.Date);
        }

        var transactions = await query
            .OrderByDescending(t => t.TransactionDate)
            .ToListAsync();


        var totalIncome = transactions.Sum(t => t.Amount);

        ViewBag.TotalIncome = totalIncome;

        // Categories for filter dropdown
        var categories = await _context.Categories
            .Where(c =>
                c.UserId == userId &&
                c.Type == TransactionType.Income)
            .OrderBy(c => c.Name)
            .ToListAsync();

        ViewBag.TotalIncome = totalIncome;
        ViewBag.Categories = categories;
        ViewBag.Search = search;
        ViewBag.CategoryId = categoryId;
        ViewBag.FromDate = fromDate?.ToString("yyyy-MM-dd");
        ViewBag.ToDate = toDate?.ToString("yyyy-MM-dd");

        return View(transactions);
    }


    // GET: /Income/Create
    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var userId = _userManager.GetUserId(User);

        if (userId == null)
        {
            return Challenge();
        }

        var categories = await _context.Categories
            .Where(c =>
                c.UserId == userId &&
                c.Type == TransactionType.Income)
            .OrderBy(c => c.Name)
            .ToListAsync();

        ViewBag.Categories = categories;

        return View(new IncomeViewModel());
    }


    // POST: /Income/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(IncomeViewModel model)
    {
        var userId = _userManager.GetUserId(User);

        if (userId == null)
        {
            return Challenge();
        }

        // Verify selected category belongs to this user
        var category = await _context.Categories
            .FirstOrDefaultAsync(c =>
                c.Id == model.CategoryId &&
                c.UserId == userId &&
                c.Type == TransactionType.Income);

        if (category == null)
        {
            ModelState.AddModelError(
                "CategoryId",
                "Please select a valid income category.");
        }

        if (!ModelState.IsValid)
        {
            ViewBag.Categories = await _context.Categories
                .Where(c =>
                    c.UserId == userId &&
                    c.Type == TransactionType.Income)
                .OrderBy(c => c.Name)
                .ToListAsync();

            return View(model);
        }

        var transaction = new Transaction
        {
            UserId = userId,
            CategoryId = model.CategoryId,
            Amount = model.Amount,
            Type = TransactionType.Income,
            Description = model.Description,
            TransactionDate = model.TransactionDate,
            CreatedAt = DateTime.UtcNow
        };

        _context.Transactions.Add(transaction);

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] =
            "Income added successfully.";

        return RedirectToAction(nameof(Index));
    }


    // GET: /Income/Edit/5
    [HttpGet]
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var userId = _userManager.GetUserId(User);

        if (userId == null)
        {
            return Challenge();
        }

        var transaction = await _context.Transactions
            .FirstOrDefaultAsync(t =>
                t.Id == id &&
                t.UserId == userId &&
                t.Type == TransactionType.Income);

        if (transaction == null)
        {
            return NotFound();
        }

        var categories = await _context.Categories
            .Where(c =>
                c.UserId == userId &&
                c.Type == TransactionType.Income)
            .OrderBy(c => c.Name)
            .ToListAsync();

        ViewBag.Categories = categories;

        var model = new IncomeViewModel
        {
            Amount = transaction.Amount,
            CategoryId = transaction.CategoryId,
            Description = transaction.Description,
            TransactionDate = transaction.TransactionDate
        };

        ViewBag.TransactionId = transaction.Id;

        return View(model);
    }


    // POST: /Income/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        int id,
        IncomeViewModel model)
    {
        var userId = _userManager.GetUserId(User);

        if (userId == null)
        {
            return Challenge();
        }

        var transaction = await _context.Transactions
            .FirstOrDefaultAsync(t =>
                t.Id == id &&
                t.UserId == userId &&
                t.Type == TransactionType.Income);

        if (transaction == null)
        {
            return NotFound();
        }

        // Verify category belongs to current user
        var category = await _context.Categories
            .FirstOrDefaultAsync(c =>
                c.Id == model.CategoryId &&
                c.UserId == userId &&
                c.Type == TransactionType.Income);

        if (category == null)
        {
            ModelState.AddModelError(
                "CategoryId",
                "Please select a valid income category.");
        }

        if (!ModelState.IsValid)
        {
            ViewBag.Categories = await _context.Categories
                .Where(c =>
                    c.UserId == userId &&
                    c.Type == TransactionType.Income)
                .OrderBy(c => c.Name)
                .ToListAsync();

            ViewBag.TransactionId = id;

            return View(model);
        }

        transaction.Amount = model.Amount;
        transaction.CategoryId = model.CategoryId;
        transaction.Description = model.Description;
        transaction.TransactionDate = model.TransactionDate;

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] =
            "Income updated successfully.";

        return RedirectToAction(nameof(Index));
    }


    // POST: /Income/Delete/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = _userManager.GetUserId(User);

        if (userId == null)
        {
            return Challenge();
        }

        var transaction = await _context.Transactions
            .FirstOrDefaultAsync(t =>
                t.Id == id &&
                t.UserId == userId &&
                t.Type == TransactionType.Income);

        if (transaction == null)
        {
            return NotFound();
        }

        _context.Transactions.Remove(transaction);

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] =
            "Income deleted successfully.";

        return RedirectToAction(nameof(Index));
    }

    // GET: /Income/CreateCategory
    // [HttpGet]
    // public IActionResult CreateCategory()
    // {
    //     return View(new CategoryViewModel
    //     {
    //         Type = TransactionType.Income
    //     });
    // }
    [HttpGet]
    public async Task<IActionResult> CreateCategory()
    {
        var userId = _userManager.GetUserId(User);

        if (userId == null)
        {
            return Challenge();
        }

        var categories = await _context.Categories
            .Where(c =>
                c.UserId == userId &&
                c.Type == TransactionType.Income)
            .OrderBy(c => c.Name)
            .ToListAsync();

        ViewBag.Categories = categories;

        return View(new CategoryViewModel
        {
            Type = TransactionType.Income
        });
    }

    // POST: /Income/CreateCategory
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateCategory(
        CategoryViewModel model)
    {
        var userId = _userManager.GetUserId(User);

        if (userId == null)
        {
            return Challenge();
        }

        model.Type = TransactionType.Income;

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        // Check whether this user already has the same category
        var categoryExists = await _context.Categories
            .AnyAsync(c =>
                c.UserId == userId &&
                c.Type == TransactionType.Income &&
                c.Name.ToLower() == model.Name.ToLower());

        if (categoryExists)
        {
            ModelState.AddModelError(
                "Name",
                "You already have an income category with this name.");

            return View(model);
        }

        var category = new Category
        {
            Name = model.Name.Trim(),
            Type = TransactionType.Income,
            UserId = userId
        };

        _context.Categories.Add(category);

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] =
            "Income category created successfully.";

        return RedirectToAction(nameof(Index));
    }


    // POST: /Income/DeleteCategory/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteCategory(int id)
    {
        var userId = _userManager.GetUserId(User);

        if (userId == null)
        {
            return Challenge();
        }

        var category = await _context.Categories
            .FirstOrDefaultAsync(c =>
                c.Id == id &&
                c.UserId == userId &&
                c.Type == TransactionType.Income);

        if (category == null)
        {
            return NotFound();
        }

        // Check whether this category is being used
        var isUsed = await _context.Transactions
            .AnyAsync(t =>
                t.CategoryId == id &&
                t.UserId == userId);

        if (isUsed)
        {
            TempData["ErrorMessage"] =
                "This category cannot be deleted because it is being used by a transaction.";

            return RedirectToAction(nameof(Index));
        }

        _context.Categories.Remove(category);

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] =
            "Income category deleted successfully.";

        return RedirectToAction(nameof(Index));
    }
}