using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using personal_finance_Tracker.Data;
using personal_finance_Tracker.Models;
using personal_finance_Tracker.ViewModels;
using personal_finance_Tracker.Services;

namespace personal_finance_Tracker.Controllers;

[Authorize]
public class TransactionController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ExcelReportService _excelReportService;
    private readonly PdfReportService _pdfReportService;

    public TransactionController(
    ApplicationDbContext context,
    UserManager<ApplicationUser> userManager,
    ExcelReportService excelReportService,
    PdfReportService pdfReportService)
    {
        _context = context;
        _userManager = userManager;
        _excelReportService = excelReportService;
        _pdfReportService = pdfReportService;
    }


    // GET: /Transaction
    // GET: /Transaction
    public async Task<IActionResult> Index(
        TransactionType? type,
        int? categoryId,
        DateTime? startDate,
        DateTime? endDate,
        string? search,
        int page = 1)
    {
        const int pageSize = 10;

        var userId = _userManager.GetUserId(User);

        if (userId == null)
        {
            return Challenge();
        }


        // =========================================================
        // Base query
        // =========================================================

        var query = _context.Transactions
            .Include(t => t.Category)
            .Where(t => t.UserId == userId)
            .AsQueryable();


        // =========================================================
        // Type filter
        // =========================================================

        if (type.HasValue)
        {
            query = query.Where(t => t.Type == type.Value);
        }


        // =========================================================
        // Category filter
        // =========================================================

        if (categoryId.HasValue)
        {
            query = query.Where(t =>
                t.CategoryId == categoryId.Value);
        }


        // =========================================================
        // Start date filter
        // =========================================================

        if (startDate.HasValue)
        {
            var start = startDate.Value.Date;

            query = query.Where(t =>
                t.TransactionDate >= start);
        }


        // =========================================================
        // End date filter
        // =========================================================

        if (endDate.HasValue)
        {
            var end = endDate.Value.Date.AddDays(1);

            query = query.Where(t =>
                t.TransactionDate < end);
        }


        // =========================================================
        // Search
        // =========================================================

        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.Trim();

            query = query.Where(t =>
                (t.Description != null &&
                 t.Description.Contains(search))
                ||
                (t.Category != null &&
                 t.Category.Name.Contains(search)));
        }


        // =========================================================
        // Summary
        // =========================================================

        var totalIncome = await query
            .Where(t => t.Type == TransactionType.Income)
            .SumAsync(t => (decimal?)t.Amount) ?? 0m;


        var totalExpense = await query
            .Where(t => t.Type == TransactionType.Expense)
            .SumAsync(t => (decimal?)t.Amount) ?? 0m;


        var totalTransactions = await query.CountAsync();


        var balance = totalIncome - totalExpense;


        // =========================================================
        // Pagination
        // =========================================================

        var totalPages = (int)Math.Ceiling(
            totalTransactions / (double)pageSize);


        if (page < 1)
        {
            page = 1;
        }

        if (totalPages > 0 && page > totalPages)
        {
            page = totalPages;
        }


        // =========================================================
        // Get current page transactions
        // =========================================================

        var transactions = await query
            .OrderByDescending(t => t.TransactionDate)
            .ThenByDescending(t => t.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();


        // =========================================================
        // Get current user's categories
        // =========================================================

        var categories = await _context.Categories
            .Where(c => c.UserId == userId)
            .OrderBy(c => c.Type)
            .ThenBy(c => c.Name)
            .ToListAsync();


        // =========================================================
        // ViewModel
        // =========================================================

        var viewModel = new TransactionViewModel
        {
            Transactions = transactions,
            Categories = categories,

            Type = type,
            CategoryId = categoryId,
            StartDate = startDate,
            EndDate = endDate,
            Search = search,

            CurrentPage = page,
            TotalPages = totalPages,

            TotalIncome = totalIncome,
            TotalExpense = totalExpense,
            Balance = balance,
            TotalTransactions = totalTransactions
        };


        return View(viewModel);
    }

    // GET: /Transaction/ExportExcel
    [HttpGet]
    public async Task<IActionResult> ExportExcel(
        TransactionType? type,
        int? categoryId,
        DateTime? startDate,
        DateTime? endDate,
        string? search)
    {
        var userId = _userManager.GetUserId(User);

        if (userId == null)
        {
            return Challenge();
        }


        // =====================================================
        // Current user's transactions only
        // =====================================================

        var query = _context.Transactions
            .Include(t => t.Category)
            .Where(t => t.UserId == userId)
            .AsQueryable();


        // =====================================================
        // Type filter
        // =====================================================

        if (type.HasValue)
        {
            query = query.Where(t =>
                t.Type == type.Value);
        }


        // =====================================================
        // Category filter
        // =====================================================

        if (categoryId.HasValue)
        {
            query = query.Where(t =>
                t.CategoryId == categoryId.Value);
        }


        // =====================================================
        // Start date
        // =====================================================

        if (startDate.HasValue)
        {
            var start = startDate.Value.Date;

            query = query.Where(t =>
                t.TransactionDate >= start);
        }


        // =====================================================
        // End date
        // =====================================================

        if (endDate.HasValue)
        {
            var end = endDate.Value.Date.AddDays(1);

            query = query.Where(t =>
                t.TransactionDate < end);
        }


        // =====================================================
        // Search
        // =====================================================

        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.Trim();

            query = query.Where(t =>
                (t.Description != null &&
                 t.Description.Contains(search))
                ||
                (t.Category != null &&
                 t.Category.Name.Contains(search)));
        }


        // =====================================================
        // Get filtered transactions
        // =====================================================

        var transactions = await query
            .OrderByDescending(t => t.TransactionDate)
            .ThenByDescending(t => t.Id)
            .ToListAsync();


        // =====================================================
        // Get current user
        // =====================================================

        var user = await _userManager.GetUserAsync(User);

        if (user == null)
        {
            return Challenge();
        }


        var userName = string.IsNullOrWhiteSpace(user.FullName)
            ? user.UserName ?? "User"
            : user.FullName;


        // =====================================================
        // Generate Excel
        // =====================================================

        var fileBytes = _excelReportService.GenerateReport(
            transactions,
            userName,
            startDate,
            endDate);


        // =====================================================
        // File name
        // =====================================================

        var fileName =
            $"Expense_Income_Report_{DateTime.Now:yyyy-MM-dd}.xlsx";


        return File(
            fileBytes,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            fileName);
    }
    // GET: /Transaction/ExportPdf
    [HttpGet]
    public async Task<IActionResult> ExportPdf(
        TransactionType? type,
        int? categoryId,
        DateTime? startDate,
        DateTime? endDate,
        string? search)
    {
        var userId = _userManager.GetUserId(User);

        if (userId == null)
        {
            return Challenge();
        }


        // Current user's transactions only

        var query = _context.Transactions
            .Include(t => t.Category)
            .Where(t => t.UserId == userId)
            .AsQueryable();


        // Type

        if (type.HasValue)
        {
            query = query.Where(t =>
                t.Type == type.Value);
        }


        // Category

        if (categoryId.HasValue)
        {
            query = query.Where(t =>
                t.CategoryId == categoryId.Value);
        }


        // Start date

        if (startDate.HasValue)
        {
            var start = startDate.Value.Date;

            query = query.Where(t =>
                t.TransactionDate >= start);
        }


        // End date

        if (endDate.HasValue)
        {
            var end = endDate.Value.Date.AddDays(1);

            query = query.Where(t =>
                t.TransactionDate < end);
        }


        // Search

        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.Trim();

            query = query.Where(t =>
                (t.Description != null &&
                 t.Description.Contains(search))
                ||
                (t.Category != null &&
                 t.Category.Name.Contains(search)));
        }


        // Get transactions

        var transactions = await query
            .OrderByDescending(t => t.TransactionDate)
            .ThenByDescending(t => t.Id)
            .ToListAsync();


        // Get user

        var user = await _userManager.GetUserAsync(User);

        if (user == null)
        {
            return Challenge();
        }


        var userName = string.IsNullOrWhiteSpace(user.FullName)
            ? user.UserName ?? "User"
            : user.FullName;


        // Generate PDF

        var fileBytes = _pdfReportService.GenerateReport(
            transactions,
            userName,
            startDate,
            endDate);


        var fileName =
            $"Expense_Income_Report_{DateTime.Now:yyyy-MM-dd}.pdf";


        return File(
            fileBytes,
            "application/pdf",
            fileName);
    }
}