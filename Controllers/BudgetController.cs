using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using personal_finance_Tracker.Data;
using personal_finance_Tracker.Models;
using personal_finance_Tracker.ViewModels;

namespace personal_finance_Tracker.Controllers;

[Authorize]
public class BudgetController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public BudgetController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    // =====================================================
    // INDEX - Show current user's budgets
    // =====================================================

    public async Task<IActionResult> Index(DateTime? month)
    {
        var userId = _userManager.GetUserId(User);

        // If no month is selected, use current month
        var selectedMonth = month ?? new DateTime(
            DateTime.Today.Year,
            DateTime.Today.Month,
            1);

        // Make sure we always work with the first day of the month
        selectedMonth = new DateTime(
            selectedMonth.Year,
            selectedMonth.Month,
            1);

        var nextMonth = selectedMonth.AddMonths(1);

        var budgets = await _context.Budgets
            .Include(b => b.Category)
            .Where(b =>
                b.UserId == userId &&
                b.Month >= selectedMonth &&
                b.Month < nextMonth)
            .OrderBy(b => b.Category.Name)
            .ToListAsync();

        var budgetData = new List<BudgetViewModel>();

        foreach (var budget in budgets)
        {
            var spent = await _context.Transactions
                .Where(t =>
                    t.UserId == userId &&
                    t.CategoryId == budget.CategoryId &&
                    t.Type == TransactionType.Expense &&
                    t.TransactionDate >= selectedMonth &&
                    t.TransactionDate < nextMonth)
                .SumAsync(t => (decimal?)t.Amount) ?? 0;

            var remaining = budget.Amount - spent;

            var percentage = budget.Amount > 0
                ? (spent / budget.Amount) * 100
                : 0;

            budgetData.Add(new BudgetViewModel
            {
                Id = budget.Id,
                CategoryName = budget.Category.Name,
                Month = budget.Month,
                Amount = budget.Amount,
                Spent = spent,
                Remaining = remaining,
                Percentage = percentage
            });
        }

        ViewBag.SelectedMonth = selectedMonth;

        return View(budgetData);
    }


    // =====================================================
    // CREATE - GET
    // =====================================================

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var userId = _userManager.GetUserId(User);

        var categories = await _context.Categories
            .Where(c => c.UserId == userId &&
                        c.Type == TransactionType.Expense)
            .OrderBy(c => c.Name)
            .ToListAsync();

        ViewBag.Categories = categories;

        return View();
    }


    // =====================================================
    // CREATE - POST
    // =====================================================

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        decimal amount,
        int categoryId,
        DateTime month)
    {
        var userId = _userManager.GetUserId(User);

        // ---------------------------------------------
        // Validate amount
        // ---------------------------------------------

        if (amount <= 0)
        {
            ModelState.AddModelError(
                "amount",
                "Budget amount must be greater than 0.");
        }

        // ---------------------------------------------
        // Normalize month
        // ---------------------------------------------

        var budgetMonth = new DateTime(
            month.Year,
            month.Month,
            1);

        // ---------------------------------------------
        // Make sure category belongs to user
        // and is an Expense category
        // ---------------------------------------------

        var category = await _context.Categories
            .FirstOrDefaultAsync(c =>
                c.Id == categoryId &&
                c.UserId == userId &&
                c.Type == TransactionType.Expense);

        if (category == null)
        {
            ModelState.AddModelError(
                "categoryId",
                "Invalid expense category.");
        }

        // ---------------------------------------------
        // Prevent duplicate budget
        // ---------------------------------------------

        var alreadyExists = await _context.Budgets
            .AnyAsync(b =>
                b.UserId == userId &&
                b.CategoryId == categoryId &&
                b.Month == budgetMonth);

        if (alreadyExists)
        {
            ModelState.AddModelError(
                "",
                "A budget already exists for this category and month.");
        }

        if (!ModelState.IsValid)
        {
            var categories = await _context.Categories
                .Where(c =>
                    c.UserId == userId &&
                    c.Type == TransactionType.Expense)
                .OrderBy(c => c.Name)
                .ToListAsync();

            ViewBag.Categories = categories;

            return View();
        }

        // ---------------------------------------------
        // Create budget
        // ---------------------------------------------

        var budget = new Budget
        {
            UserId = userId!,
            CategoryId = categoryId,
            Amount = amount,
            Month = budgetMonth
        };

        _context.Budgets.Add(budget);

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] =
            "Budget created successfully.";

        return RedirectToAction(nameof(Index));
    }


    // =====================================================
    // EDIT - GET
    // =====================================================

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var userId = _userManager.GetUserId(User);

        var budget = await _context.Budgets
            .Include(b => b.Category)
            .FirstOrDefaultAsync(b =>
                b.Id == id &&
                b.UserId == userId);

        if (budget == null)
        {
            return NotFound();
        }

        var categories = await _context.Categories
            .Where(c =>
                c.UserId == userId &&
                c.Type == TransactionType.Expense)
            .OrderBy(c => c.Name)
            .ToListAsync();

        ViewBag.Categories = categories;

        return View(budget);
    }


    // =====================================================
    // EDIT - POST
    // =====================================================

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        int id,
        decimal amount,
        int categoryId,
        DateTime month)
    {
        var userId = _userManager.GetUserId(User);

        // ---------------------------------------------
        // Find ONLY user's own budget
        // ---------------------------------------------

        var budget = await _context.Budgets
            .FirstOrDefaultAsync(b =>
                b.Id == id &&
                b.UserId == userId);

        if (budget == null)
        {
            return NotFound();
        }

        // ---------------------------------------------
        // Validate amount
        // ---------------------------------------------

        if (amount <= 0)
        {
            ModelState.AddModelError(
                "amount",
                "Budget amount must be greater than 0.");
        }

        // ---------------------------------------------
        // Normalize month
        // ---------------------------------------------

        var budgetMonth = new DateTime(
            month.Year,
            month.Month,
            1);

        // ---------------------------------------------
        // Validate category
        // ---------------------------------------------

        var category = await _context.Categories
            .FirstOrDefaultAsync(c =>
                c.Id == categoryId &&
                c.UserId == userId &&
                c.Type == TransactionType.Expense);

        if (category == null)
        {
            ModelState.AddModelError(
                "categoryId",
                "Invalid expense category.");
        }

        // ---------------------------------------------
        // Check duplicate budget
        // ---------------------------------------------

        var alreadyExists = await _context.Budgets
            .AnyAsync(b =>
                b.Id != id &&
                b.UserId == userId &&
                b.CategoryId == categoryId &&
                b.Month == budgetMonth);

        if (alreadyExists)
        {
            ModelState.AddModelError(
                "",
                "A budget already exists for this category and month.");
        }

        if (!ModelState.IsValid)
        {
            var categories = await _context.Categories
                .Where(c =>
                    c.UserId == userId &&
                    c.Type == TransactionType.Expense)
                .OrderBy(c => c.Name)
                .ToListAsync();

            ViewBag.Categories = categories;

            return View(budget);
        }

        // ---------------------------------------------
        // Update budget
        // ---------------------------------------------

        budget.Amount = amount;
        budget.CategoryId = categoryId;
        budget.Month = budgetMonth;

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] =
            "Budget updated successfully.";

        return RedirectToAction(nameof(Index));
    }


    // =====================================================
    // DELETE - POST
    // =====================================================

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = _userManager.GetUserId(User);

        // ---------------------------------------------
        // Find ONLY user's own budget
        // ---------------------------------------------

        var budget = await _context.Budgets
            .FirstOrDefaultAsync(b =>
                b.Id == id &&
                b.UserId == userId);

        if (budget == null)
        {
            return NotFound();
        }

        _context.Budgets.Remove(budget);

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] =
            "Budget deleted successfully.";

        return RedirectToAction(nameof(Index));
    }
}