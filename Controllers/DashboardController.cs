// using Microsoft.AspNetCore.Authorization;
// using Microsoft.AspNetCore.Identity;
// using Microsoft.AspNetCore.Mvc;
// using Microsoft.EntityFrameworkCore;
// using personal_finance_Tracker.Data;
// using personal_finance_Tracker.Models;
// using personal_finance_Tracker.ViewModels;

// namespace personal_finance_Tracker.Controllers;

// [Authorize]
// public class DashboardController : Controller
// {
//     private readonly ApplicationDbContext _context;
//     private readonly UserManager<ApplicationUser> _userManager;

//     public DashboardController(
//         ApplicationDbContext context,
//         UserManager<ApplicationUser> userManager)
//     {
//         _context = context;
//         _userManager = userManager;
//     }


//     // =====================================================
//     // DASHBOARD
//     // =====================================================

//     public async Task<IActionResult> Index()
//     {
//         var userId = _userManager.GetUserId(User);

//         if (userId == null)
//         {
//             return Challenge();
//         }


//         // =====================================================
//         // TRANSACTIONS
//         // =====================================================

//         var transactions = _context.Transactions
//             .Where(t => t.UserId == userId);


//         // Total Income

//         var totalIncome = await transactions
//             .Where(t => t.Type == TransactionType.Income)
//             .SumAsync(t => (decimal?)t.Amount) ?? 0;


//         // Total Expense

//         var totalExpense = await transactions
//             .Where(t => t.Type == TransactionType.Expense)
//             .SumAsync(t => (decimal?)t.Amount) ?? 0;


//         // Total Transactions

//         var totalTransactions = await transactions
//             .CountAsync();


//         // =====================================================
//         // RECENT TRANSACTIONS
//         // =====================================================

//         var recentTransactions = await _context.Transactions
//             .Include(t => t.Category)
//             .Where(t => t.UserId == userId)
//             .OrderByDescending(t => t.TransactionDate)
//             .ThenByDescending(t => t.Id)
//             .Take(5)
//             .ToListAsync();



//         // =====================================================
//         // CURRENT MONTH
//         // =====================================================

//         var currentMonth = new DateTime(
//             DateTime.Today.Year,
//             DateTime.Today.Month,
//             1);

//         var nextMonth = currentMonth.AddMonths(1);

//         // =====================================================
//         // MONTHLY CHART - LAST 6 MONTHS
//         // =====================================================

//         var chartMonths = new List<string>();

//         var monthlyIncome = new List<decimal>();

//         var monthlyExpense = new List<decimal>();


//         // First day of the month 5 months ago

//         var chartStartMonth = currentMonth.AddMonths(-5);


//         for (int i = 0; i < 6; i++)
//         {
//             var monthStart = chartStartMonth.AddMonths(i);

//             var monthEnd = monthStart.AddMonths(1);


//             var income = await _context.Transactions
//                 .Where(t =>
//                     t.UserId == userId &&
//                     t.Type == TransactionType.Income &&
//                     t.TransactionDate >= monthStart &&
//                     t.TransactionDate < monthEnd)
//                 .SumAsync(t => (decimal?)t.Amount) ?? 0;


//             var expense = await _context.Transactions
//                 .Where(t =>
//                     t.UserId == userId &&
//                     t.Type == TransactionType.Expense &&
//                     t.TransactionDate >= monthStart &&
//                     t.TransactionDate < monthEnd)
//                 .SumAsync(t => (decimal?)t.Amount) ?? 0;


//             chartMonths.Add(monthStart.ToString("MMM"));

//             monthlyIncome.Add(income);

//             monthlyExpense.Add(expense);
//         }


//         // =====================================================
//         // CURRENT MONTH BUDGETS
//         // =====================================================

//         var budgets = await _context.Budgets
//             .Include(b => b.Category)
//             .Where(b =>
//                 b.UserId == userId &&
//                 b.Month >= currentMonth &&
//                 b.Month < nextMonth)
//             .OrderBy(b => b.Category.Name)
//             .ToListAsync();


//         // =====================================================
//         // BUDGET DASHBOARD DATA
//         // =====================================================

//         var budgetData = new List<DashboardBudgetViewModel>();

//         foreach (var budget in budgets)
//         {
//             var spent = await _context.Transactions
//                 .Where(t =>
//                     t.UserId == userId &&
//                     t.CategoryId == budget.CategoryId &&
//                     t.Type == TransactionType.Expense &&
//                     t.TransactionDate >= currentMonth &&
//                     t.TransactionDate < nextMonth)
//                 .SumAsync(t => (decimal?)t.Amount) ?? 0;


//             var remaining = budget.Amount - spent;


//             var percentage = budget.Amount > 0
//                 ? (spent / budget.Amount) * 100
//                 : 0;


//             budgetData.Add(new DashboardBudgetViewModel
//             {
//                 CategoryName = budget.Category.Name,

//                 BudgetAmount = budget.Amount,

//                 SpentAmount = spent,

//                 RemainingAmount = remaining,

//                 Percentage = percentage
//             });
//         }


//         // =====================================================
//         // BUDGET SUMMARY
//         // =====================================================

//         var totalBudget = budgetData
//             .Sum(b => b.BudgetAmount);

//         var totalBudgetSpent = budgetData
//             .Sum(b => b.SpentAmount);

//         var totalBudgetRemaining = totalBudget - totalBudgetSpent;


//         // =====================================================
//         // VIEW MODEL
//         // =====================================================

//         var viewModel = new DashboardViewModel
//         {
//             // Summary

//             TotalIncome = totalIncome,

//             TotalExpense = totalExpense,

//             Balance = totalIncome - totalExpense,

//             TotalTransactions = totalTransactions,


//             // Budget

//             TotalBudget = totalBudget,

//             TotalBudgetSpent = totalBudgetSpent,

//             TotalBudgetRemaining = totalBudgetRemaining,


//             // Recent transactions

//             RecentTransactions = recentTransactions,


//             // Chart

//             ChartIncome = totalIncome,

//             ChartExpense = totalExpense,

//             ChartMonths = chartMonths,

//             MonthlyIncome = monthlyIncome,

//             MonthlyExpense = monthlyExpense,


//             // Budget list

//             Budgets = budgetData
//         };


//         return View(viewModel);
//     }
// }



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


    // =====================================================
    // DASHBOARD
    // =====================================================

    public async Task<IActionResult> Index()
    {
        var userId = _userManager.GetUserId(User);

        if (userId == null)
        {
            return Challenge();
        }


        // =====================================================
        // DATE RANGE
        // =====================================================

        var today = DateTime.Today;

        var currentMonth = new DateTime(
            today.Year,
            today.Month,
            1);

        var nextMonth = currentMonth.AddMonths(1);

        var chartStartMonth = currentMonth.AddMonths(-5);


        // =====================================================
        // USER TRANSACTIONS
        // =====================================================

        var transactions = _context.Transactions
            .Where(t => t.UserId == userId);


        // =====================================================
        // OVERALL TOTALS
        // =====================================================

        var totalIncome = await transactions
            .Where(t => t.Type == TransactionType.Income)
            .SumAsync(t => (decimal?)t.Amount) ?? 0;


        var totalExpense = await transactions
            .Where(t => t.Type == TransactionType.Expense)
            .SumAsync(t => (decimal?)t.Amount) ?? 0;


        var totalTransactions = await transactions
            .CountAsync();


        var balance = totalIncome - totalExpense;


        // =====================================================
        // CURRENT MONTH TOTALS
        // =====================================================

        var currentMonthIncome = await transactions
            .Where(t =>
                t.Type == TransactionType.Income &&
                t.TransactionDate >= currentMonth &&
                t.TransactionDate < nextMonth)
            .SumAsync(t => (decimal?)t.Amount) ?? 0;


        var currentMonthExpense = await transactions
            .Where(t =>
                t.Type == TransactionType.Expense &&
                t.TransactionDate >= currentMonth &&
                t.TransactionDate < nextMonth)
            .SumAsync(t => (decimal?)t.Amount) ?? 0;


        var currentMonthBalance =
            currentMonthIncome - currentMonthExpense;


        var savingsRate = currentMonthIncome > 0
            ? (currentMonthBalance / currentMonthIncome) * 100
            : 0;


        // =====================================================
        // RECENT TRANSACTIONS
        // =====================================================

        var recentTransactions = await _context.Transactions
            .AsNoTracking()
            .Include(t => t.Category)
            .Where(t => t.UserId == userId)
            .OrderByDescending(t => t.TransactionDate)
            .ThenByDescending(t => t.Id)
            .Take(5)
            .ToListAsync();


        // =====================================================
        // MONTHLY CHART
        // =====================================================

        var monthlyData = await _context.Transactions
            .AsNoTracking()
            .Where(t =>
                t.UserId == userId &&
                t.TransactionDate >= chartStartMonth &&
                t.TransactionDate < nextMonth)
            .GroupBy(t => new
            {
                t.TransactionDate.Year,
                t.TransactionDate.Month,
                t.Type
            })
            .Select(g => new
            {
                g.Key.Year,
                g.Key.Month,
                g.Key.Type,
                Total = g.Sum(t => t.Amount)
            })
            .ToListAsync();


        var chartMonths = new List<string>();

        var monthlyIncome = new List<decimal>();

        var monthlyExpense = new List<decimal>();


        for (int i = 0; i < 6; i++)
        {
            var month = chartStartMonth.AddMonths(i);

            chartMonths.Add(month.ToString("MMM"));


            var income = monthlyData
                .Where(x =>
                    x.Year == month.Year &&
                    x.Month == month.Month &&
                    x.Type == TransactionType.Income)
                .Select(x => x.Total)
                .FirstOrDefault();


            var expense = monthlyData
                .Where(x =>
                    x.Year == month.Year &&
                    x.Month == month.Month &&
                    x.Type == TransactionType.Expense)
                .Select(x => x.Total)
                .FirstOrDefault();


            monthlyIncome.Add(income);

            monthlyExpense.Add(expense);
        }


        // =====================================================
        // CURRENT MONTH BUDGETS
        // =====================================================

        var budgets = await _context.Budgets
            .AsNoTracking()
            .Include(b => b.Category)
            .Where(b =>
                b.UserId == userId &&
                b.Month >= currentMonth &&
                b.Month < nextMonth)
            .OrderBy(b => b.Category.Name)
            .ToListAsync();


        // =====================================================
        // BUDGET SPENDING
        // =====================================================

        var budgetCategoryIds = budgets
            .Select(b => b.CategoryId)
            .ToList();


        var budgetSpending = await _context.Transactions
            .AsNoTracking()
            .Where(t =>
                t.UserId == userId &&
                t.Type == TransactionType.Expense &&
                budgetCategoryIds.Contains(t.CategoryId) &&
                t.TransactionDate >= currentMonth &&
                t.TransactionDate < nextMonth)
            .GroupBy(t => t.CategoryId)
            .Select(g => new
            {
                CategoryId = g.Key,
                Spent = g.Sum(t => t.Amount)
            })
            .ToListAsync();


        // =====================================================
        // BUDGET VIEW MODEL
        // =====================================================

        var budgetData = new List<DashboardBudgetViewModel>();


        foreach (var budget in budgets)
        {
            var spent = budgetSpending
                .Where(x => x.CategoryId == budget.CategoryId)
                .Select(x => x.Spent)
                .FirstOrDefault();


            var remaining = budget.Amount - spent;


            var percentage = budget.Amount > 0
                ? (spent / budget.Amount) * 100
                : 0;


            budgetData.Add(new DashboardBudgetViewModel
            {
                CategoryName = budget.Category.Name,

                BudgetAmount = budget.Amount,

                SpentAmount = spent,

                RemainingAmount = remaining,

                Percentage = percentage
            });
        }


        // =====================================================
        // BUDGET SUMMARY
        // =====================================================

        var totalBudget = budgetData
            .Sum(b => b.BudgetAmount);


        var totalBudgetSpent = budgetData
            .Sum(b => b.SpentAmount);


        var totalBudgetRemaining =
            totalBudget - totalBudgetSpent;


        // =====================================================
        // VIEW MODEL
        // =====================================================

        var viewModel = new DashboardViewModel
        {
            // Overall

            TotalIncome = totalIncome,

            TotalExpense = totalExpense,

            Balance = balance,

            TotalTransactions = totalTransactions,


            // Current month

            CurrentMonthIncome = currentMonthIncome,

            CurrentMonthExpense = currentMonthExpense,

            CurrentMonthBalance = currentMonthBalance,

            SavingsRate = savingsRate,


            // Budget

            TotalBudget = totalBudget,

            TotalBudgetSpent = totalBudgetSpent,

            TotalBudgetRemaining = totalBudgetRemaining,


            // Recent transactions

            RecentTransactions = recentTransactions,


            // Doughnut chart

            ChartIncome = totalIncome,

            ChartExpense = totalExpense,


            // Monthly chart

            ChartMonths = chartMonths,

            MonthlyIncome = monthlyIncome,

            MonthlyExpense = monthlyExpense,


            // Budget overview

            Budgets = budgetData
        };


        return View(viewModel);
    }
}