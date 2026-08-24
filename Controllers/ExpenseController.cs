using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Rendering;
using personal_finance_Tracker.Data;
using personal_finance_Tracker.Models;
using personal_finance_Tracker.ViewModels;

namespace personal_finance_Tracker.Controllers;

[Authorize]
public class ExpenseController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public ExpenseController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }


    // GET: /Expense
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
                t.Type == TransactionType.Expense)
            .AsQueryable();


        // Search
        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(t =>
                t.Description != null &&
                t.Description.Contains(search));
        }


        // Category filter
        if (categoryId.HasValue)
        {
            query = query.Where(t =>
                t.CategoryId == categoryId.Value);
        }


        // From date
        if (fromDate.HasValue)
        {
            query = query.Where(t =>
                t.TransactionDate.Date >= fromDate.Value.Date);
        }


        // To date
        if (toDate.HasValue)
        {
            query = query.Where(t =>
                t.TransactionDate.Date <= toDate.Value.Date);
        }


        var transactions = await query
            .OrderByDescending(t => t.TransactionDate)
            .ToListAsync();


        var totalExpense = transactions.Sum(t => t.Amount);

        ViewBag.TotalExpense = totalExpense;


        // User's expense categories
        var categories = await _context.Categories
            .Where(c =>
                c.UserId == userId &&
                c.Type == TransactionType.Expense)
            .OrderBy(c => c.Name)
            .ToListAsync();


        ViewBag.Categories = categories;
        ViewBag.Search = search;
        ViewBag.CategoryId = categoryId;
        ViewBag.FromDate = fromDate?.ToString("yyyy-MM-dd");
        ViewBag.ToDate = toDate?.ToString("yyyy-MM-dd");


        return View(transactions);
    }


    // GET: /Expense/Create
    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var userId = _userManager.GetUserId(User);

        if (userId == null)
        {
            return Challenge();
        }


        await LoadCategories(userId);

        var transaction = new Transaction
        {
            Type = TransactionType.Expense,
            TransactionDate = DateTime.Today
        };

        return View(transaction);
    }


    // POST: /Expense/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Transaction transaction)
    {
        var userId = _userManager.GetUserId(User);

        if (userId == null)
        {
            return Challenge();
        }


        transaction.Type = TransactionType.Expense;
        transaction.UserId = userId;
        transaction.CreatedAt = DateTime.UtcNow;


        // Make sure the selected category belongs to
        // the logged-in user and is an Expense category.
        var categoryExists = await _context.Categories
            .AnyAsync(c =>
                c.Id == transaction.CategoryId &&
                c.UserId == userId &&
                c.Type == TransactionType.Expense);


        if (!categoryExists)
        {
            ModelState.AddModelError(
                "CategoryId",
                "Please select a valid expense category.");
        }


        if (!ModelState.IsValid)
        {
            await LoadCategories(userId);
            return View(transaction);
        }
        // if (!ModelState.IsValid)
        // {
        //     foreach (var modelState in ModelState)
        //     {
        //         foreach (var error in modelState.Value.Errors)
        //         {
        //             Console.WriteLine(
        //                 $"VALIDATION ERROR: {modelState.Key} - {error.ErrorMessage}");
        //         }
        //     }

        //     await LoadCategories(userId);
        //     return View(transaction);
        // }


        _context.Transactions.Add(transaction);

        await _context.SaveChangesAsync();


        TempData["SuccessMessage"] =
            "Expense added successfully.";


        return RedirectToAction(nameof(Index));
    }


    // GET: /Expense/Edit/5
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
                t.Type == TransactionType.Expense);


        if (transaction == null)
        {
            return NotFound();
        }


        await LoadCategories(userId);

        return View(transaction);
    }


    // POST: /Expense/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        int id,
        Transaction transaction)
    {
        if (id != transaction.Id)
        {
            return NotFound();
        }


        var userId = _userManager.GetUserId(User);

        if (userId == null)
        {
            return Challenge();
        }


        var existingTransaction = await _context.Transactions
            .FirstOrDefaultAsync(t =>
                t.Id == id &&
                t.UserId == userId &&
                t.Type == TransactionType.Expense);


        if (existingTransaction == null)
        {
            return NotFound();
        }


        // Verify category ownership
        var categoryExists = await _context.Categories
            .AnyAsync(c =>
                c.Id == transaction.CategoryId &&
                c.UserId == userId &&
                c.Type == TransactionType.Expense);


        if (!categoryExists)
        {
            ModelState.AddModelError(
                "CategoryId",
                "Please select a valid expense category.");
        }


        if (!ModelState.IsValid)
        {
            await LoadCategories(userId);
            return View(transaction);
        }


        existingTransaction.Amount = transaction.Amount;
        existingTransaction.CategoryId = transaction.CategoryId;
        existingTransaction.Description = transaction.Description;
        existingTransaction.TransactionDate =
            transaction.TransactionDate;


        await _context.SaveChangesAsync();


        TempData["SuccessMessage"] =
            "Expense updated successfully.";


        return RedirectToAction(nameof(Index));
    }


    // GET: /Expense/Delete/5
    [HttpGet]
    public async Task<IActionResult> Delete(int? id)
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
            .Include(t => t.Category)
            .FirstOrDefaultAsync(t =>
                t.Id == id &&
                t.UserId == userId &&
                t.Type == TransactionType.Expense);


        if (transaction == null)
        {
            return NotFound();
        }


        return View(transaction);
    }


    // POST: /Expense/Delete/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
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
                t.Type == TransactionType.Expense);


        if (transaction == null)
        {
            return NotFound();
        }


        _context.Transactions.Remove(transaction);

        await _context.SaveChangesAsync();


        TempData["SuccessMessage"] =
            "Expense deleted successfully.";


        return RedirectToAction(nameof(Index));
    }


    // GET: /Expense/CreateCategory
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
                c.Type == TransactionType.Expense)
            .OrderBy(c => c.Name)
            .ToListAsync();


        ViewBag.Categories = categories;


        return View(new CategoryViewModel
        {
            Type = TransactionType.Expense
        });
    }


    // POST: /Expense/CreateCategory
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


        model.Type = TransactionType.Expense;


        if (!ModelState.IsValid)
        {
            await LoadExpenseCategories(userId);
            return View(model);
        }


        var categoryExists = await _context.Categories
            .AnyAsync(c =>
                c.UserId == userId &&
                c.Type == TransactionType.Expense &&
                c.Name.ToLower() == model.Name.ToLower());


        if (categoryExists)
        {
            ModelState.AddModelError(
                "Name",
                "You already have an expense category with this name.");

            await LoadExpenseCategories(userId);

            return View(model);
        }


        var category = new Category
        {
            Name = model.Name.Trim(),
            Type = TransactionType.Expense,
            UserId = userId
        };


        _context.Categories.Add(category);

        await _context.SaveChangesAsync();


        TempData["SuccessMessage"] =
            "Expense category created successfully.";


        return RedirectToAction(nameof(CreateCategory));
    }


    // POST: /Expense/DeleteCategory/5
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
                c.Type == TransactionType.Expense);


        if (category == null)
        {
            return NotFound();
        }


        var isUsed = await _context.Transactions
            .AnyAsync(t =>
                t.CategoryId == id &&
                t.UserId == userId);


        if (isUsed)
        {
            TempData["ErrorMessage"] =
                "This category cannot be deleted because it is being used by a transaction.";

            return RedirectToAction(nameof(CreateCategory));
        }


        _context.Categories.Remove(category);

        await _context.SaveChangesAsync();


        TempData["SuccessMessage"] =
            "Expense category deleted successfully.";


        return RedirectToAction(nameof(CreateCategory));
    }


    // Load categories for dropdown
    private async Task LoadCategories(string userId)
    {
        var categories = await _context.Categories
            .Where(c =>
                c.UserId == userId &&
                c.Type == TransactionType.Expense)
            .OrderBy(c => c.Name)
            .ToListAsync();


        ViewBag.CategoryId = new SelectList(
            categories,
            "Id",
            "Name");
    }


    // Load categories for category management page
    private async Task LoadExpenseCategories(string userId)
    {
        ViewBag.Categories = await _context.Categories
            .Where(c =>
                c.UserId == userId &&
                c.Type == TransactionType.Expense)
            .OrderBy(c => c.Name)
            .ToListAsync();
    }
}