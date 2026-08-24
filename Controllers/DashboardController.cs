using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using personal_finance_Tracker.Data;
using personal_finance_Tracker.Models;
using personal_finance_Tracker.ViewModels;

namespace personal_finance_Tracker.Controllers;

[Authorize]
public class DashboardController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public DashboardController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }


    // GET: /Dashboard
    public async Task<IActionResult> Index()
    {
        var userId = _userManager.GetUserId(User);

        if (userId == null)
        {
            return Challenge();
        }


        // Current user's transactions only

        var transactions = _context.Transactions
            .Where(t => t.UserId == userId);


        // Total Income

        var totalIncome = await transactions
            .Where(t => t.Type == TransactionType.Income)
            .SumAsync(t => (decimal?)t.Amount) ?? 0;


        // Total Expense

        var totalExpense = await transactions
            .Where(t => t.Type == TransactionType.Expense)
            .SumAsync(t => (decimal?)t.Amount) ?? 0;


        // Total Transactions

        var totalTransactions = await transactions
            .CountAsync();


        // Recent Transactions

        var recentTransactions = await _context.Transactions
            .Include(t => t.Category)
            .Where(t => t.UserId == userId)
            .OrderByDescending(t => t.TransactionDate)
            .ThenByDescending(t => t.Id)
            .Take(5)
            .ToListAsync();


        var viewModel = new DashboardViewModel
        {
            TotalIncome = totalIncome,

            TotalExpense = totalExpense,

            Balance = totalIncome - totalExpense,

            TotalTransactions = totalTransactions,

            RecentTransactions = recentTransactions,

            ChartIncome = totalIncome,

            ChartExpense = totalExpense
        };


        return View(viewModel);
    }
}