using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using personal_finance_Tracker.Data;
using personal_finance_Tracker.Models;
using personal_finance_Tracker.ViewModels;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace personal_finance_Tracker.Controllers;

[Authorize]
public class ReportsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public ReportsController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    // =========================================================
    // REPORT
    // =========================================================

    [HttpGet]
    public async Task<IActionResult> Index(
        DateTime? startDate,
        DateTime? endDate)
    {
        var userId = _userManager.GetUserId(User);

        if (userId == null)
        {
            return Challenge();
        }

        var (selectedStartDate, selectedEndDate) =
            GetReportDateRange(startDate, endDate);

        var queryEndDate = selectedEndDate.AddDays(1);

        // Get only current user's transactions
        var transactions = _context.Transactions
            .AsNoTracking()
            .Where(t =>
                t.UserId == userId &&
                t.TransactionDate >= selectedStartDate &&
                t.TransactionDate < queryEndDate);

        // =====================================================
        // SUMMARY
        // =====================================================

        var totalIncome = await transactions
            .Where(t => t.Type == TransactionType.Income)
            .SumAsync(t => (decimal?)t.Amount) ?? 0;

        var totalExpense = await transactions
            .Where(t => t.Type == TransactionType.Expense)
            .SumAsync(t => (decimal?)t.Amount) ?? 0;

        var totalTransactions = await transactions.CountAsync();

        var balance = totalIncome - totalExpense;

        var savingsRate = totalIncome > 0
            ? (balance / totalIncome) * 100
            : 0;

        // =====================================================
        // EXPENSE BY CATEGORY
        // =====================================================

        var categoryExpenses = await transactions
            .Where(t =>
                t.Type == TransactionType.Expense &&
                t.Category != null)
            .GroupBy(t => t.Category!.Name)
            .Select(g => new CategoryExpenseViewModel
            {
                CategoryName = g.Key,
                Amount = g.Sum(t => t.Amount)
            })
            .OrderByDescending(x => x.Amount)
            .ToListAsync();

        // Calculate percentage
        foreach (var category in categoryExpenses)
        {
            category.Percentage = totalExpense > 0
                ? (category.Amount / totalExpense) * 100
                : 0;
        }

        // Highest expense category
        var highestExpenseCategory =
            categoryExpenses.FirstOrDefault();

        // =====================================================
        // MONTHLY REPORT
        // =====================================================

        var monthlyTransactions = await transactions
            .Select(t => new
            {
                t.TransactionDate,
                t.Type,
                t.Amount
            })
            .ToListAsync();

        var monthlyData = monthlyTransactions
            .GroupBy(t => new
            {
                t.TransactionDate.Year,
                t.TransactionDate.Month
            })
            .OrderBy(g => g.Key.Year)
            .ThenBy(g => g.Key.Month)
            .Select(g =>
            {
                var income = g
                    .Where(t => t.Type == TransactionType.Income)
                    .Sum(t => t.Amount);

                var expense = g
                    .Where(t => t.Type == TransactionType.Expense)
                    .Sum(t => t.Amount);

                return new MonthlyReportViewModel
                {
                    Month = new DateTime(
                        g.Key.Year,
                        g.Key.Month,
                        1)
                        .ToString("MMM yyyy"),

                    Income = income,

                    Expense = expense,

                    Balance = income - expense
                };
            })
            .ToList();

        // =====================================================
        // VIEW MODEL
        // =====================================================

        var viewModel = new ReportViewModel
        {
            StartDate = selectedStartDate,
            EndDate = selectedEndDate,

            TotalIncome = totalIncome,
            TotalExpense = totalExpense,
            Balance = balance,
            TotalTransactions = totalTransactions,

            SavingsRate = savingsRate,

            HighestExpenseCategory =
                highestExpenseCategory?.CategoryName
                ?? "No expenses",

            HighestExpenseAmount =
                highestExpenseCategory?.Amount ?? 0,

            ChartIncome = totalIncome,
            ChartExpense = totalExpense,

            CategoryExpenses = categoryExpenses,

            MonthlyData = monthlyData
        };

        return View(viewModel);
    }


    // =========================================================
    // EXCEL EXPORT
    // =========================================================

    [HttpGet]
    public async Task<IActionResult> ExportExcel(
    DateTime? startDate,
    DateTime? endDate)
    {
        var userId = _userManager.GetUserId(User);

        if (userId == null)
        {
            return Challenge();
        }

        var (selectedStartDate, selectedEndDate) =
            GetReportDateRange(startDate, endDate);

        var queryEndDate = selectedEndDate.AddDays(1);

        // =========================================================
        // GET TRANSACTIONS
        // =========================================================

        var transactions = await _context.Transactions
            .AsNoTracking()
            .Include(t => t.Category)
            .Where(t =>
                t.UserId == userId &&
                t.TransactionDate >= selectedStartDate &&
                t.TransactionDate < queryEndDate)
            .OrderBy(t => t.TransactionDate)
            .ToListAsync();

        // =========================================================
        // SUMMARY CALCULATIONS
        // =========================================================

        var totalIncome = transactions
            .Where(t => t.Type == TransactionType.Income)
            .Sum(t => t.Amount);

        var totalExpense = transactions
            .Where(t => t.Type == TransactionType.Expense)
            .Sum(t => t.Amount);

        var balance = totalIncome - totalExpense;

        var savingsRate = totalIncome > 0
            ? (balance / totalIncome) * 100
            : 0;

        // =========================================================
        // CATEGORY DATA
        // =========================================================

        var categoryData = transactions
            .Where(t =>
                t.Type == TransactionType.Expense &&
                t.Category != null)
            .GroupBy(t => t.Category!.Name)
            .Select(g => new
            {
                Name = g.Key,
                Amount = g.Sum(t => t.Amount)
            })
            .OrderByDescending(x => x.Amount)
            .ToList();

        // =========================================================
        // MONTHLY DATA
        // =========================================================

        var monthlyData = transactions
            .GroupBy(t => new
            {
                t.TransactionDate.Year,
                t.TransactionDate.Month
            })
            .OrderBy(g => g.Key.Year)
            .ThenBy(g => g.Key.Month)
            .Select(g =>
            {
                var income = g
                    .Where(t => t.Type == TransactionType.Income)
                    .Sum(t => t.Amount);

                var expense = g
                    .Where(t => t.Type == TransactionType.Expense)
                    .Sum(t => t.Amount);

                return new
                {
                    Month = new DateTime(
                        g.Key.Year,
                        g.Key.Month,
                        1),

                    Income = income,
                    Expense = expense,
                    Balance = income - expense
                };
            })
            .ToList();

        // =========================================================
        // CREATE WORKBOOK
        // =========================================================

        using var workbook = new XLWorkbook();

        // =========================================================
        // COLORS / STYLES
        // =========================================================

        var headerColor = XLColor.FromHtml("#1F2937");
        var accentColor = XLColor.FromHtml("#2563EB");
        var lightColor = XLColor.FromHtml("#F3F4F6");
        var incomeColor = XLColor.FromHtml("#198754");
        var expenseColor = XLColor.FromHtml("#DC3545");
        var borderColor = XLColor.FromHtml("#D1D5DB");

        // =========================================================
        // SHEET 1: SUMMARY
        // =========================================================

        var summary = workbook.Worksheets.Add("Summary");


        // Title
        summary.Cell("A1").Value = "FinTrack";
        summary.Cell("A1").Style.Font.Bold = true;
        summary.Cell("A1").Style.Font.FontSize = 24;

        summary.Cell("A2").Value = "Financial Report";
        summary.Cell("A2").Style.Font.Bold = true;
        summary.Cell("A2").Style.Font.FontSize = 16;

        summary.Cell("A3").Value =
            $"Period: {selectedStartDate:dd MMM yyyy} - {selectedEndDate:dd MMM yyyy}";

        summary.Cell("A3").Style.Font.FontColor =
            XLColor.Gray;

        summary.Range("A1:D1").Merge();
        summary.Range("A2:D2").Merge();
        summary.Range("A3:D3").Merge();

        // Summary heading
        summary.Cell("A5").Value = "Financial Summary";

        summary.Range("A5:B5").Merge();

        summary.Range("A5:B5").Style.Fill.BackgroundColor = accentColor;

        summary.Range("A5:B5").Style.Font.FontColor =
            XLColor.White;

        summary.Range("A5:B5").Style.Font.Bold = true;

        // Summary values
        summary.Cell("A6").Value = "Total Income";
        summary.Cell("B6").Value = totalIncome;

        summary.Cell("A7").Value = "Total Expense";
        summary.Cell("B7").Value = totalExpense;

        summary.Cell("A8").Value = "Net Balance";
        summary.Cell("B8").Value = balance;

        summary.Cell("A9").Value = "Savings Rate";
        summary.Cell("B9").Value = savingsRate / 100;

        summary.Cell("A10").Value = "Transactions";
        summary.Cell("B10").Value = transactions.Count;

        // Formatting
        summary.Range("A6:B10")
            .Style.Border.OutsideBorder =
            XLBorderStyleValues.Thin;

        summary.Range("A6:B10")
            .Style.Border.OutsideBorderColor =
            borderColor;

        summary.Range("A6:B10")
            .Style.Border.InsideBorder =
            XLBorderStyleValues.Thin;

        summary.Range("A6:B10")
            .Style.Border.InsideBorderColor =
            borderColor;

        summary.Range("A6:A10")
            .Style.Font.Bold = true;

        summary.Range("B6:B8")
            .Style.NumberFormat.Format =
            "₹#,##0.00";

        summary.Cell("B9")
            .Style.NumberFormat.Format =
            "0.00%";

        summary.Cell("B6")
            .Style.Font.FontColor = incomeColor;

        summary.Cell("B7")
            .Style.Font.FontColor = expenseColor;

        summary.Cell("B8")
            .Style.Font.Bold = true;

        // =========================================================
        // HIGHEST EXPENSE
        // =========================================================

        summary.Cell("D5").Value =
            "Highest Expense Category";

        summary.Range("D5:E5").Merge();

        summary.Range("D5:E5").Style.Fill.BackgroundColor = accentColor;

        summary.Range("D5:E5").Style.Font.FontColor =
            XLColor.White;

        summary.Range("D5:E5").Style.Font.Bold = true;

        if (categoryData.Any())
        {
            summary.Cell("D6").Value =
                categoryData.First().Name;

            summary.Cell("D7").Value =
                categoryData.First().Amount;

            summary.Cell("D7")
                .Style.NumberFormat.Format =
                "₹#,##0.00";

            summary.Cell("D6").Style.Font.Bold = true;
            summary.Cell("D7").Style.Font.Bold = true;
        }
        else
        {
            summary.Cell("D6").Value =
                "No expenses";

            summary.Cell("D7").Value = 0;

            summary.Cell("D7")
                .Style.NumberFormat.Format =
                "₹#,##0.00";
        }

        // =========================================================
        // SHEET 2: TRANSACTIONS
        // =========================================================

        var transactionSheet =
            workbook.Worksheets.Add("Transactions");


        transactionSheet.Cell("A1").Value =
            "FinTrack Transactions";

        transactionSheet.Range("A1:F1").Merge();

        transactionSheet.Range("A1:F1").Style.Fill.BackgroundColor = headerColor;

        transactionSheet.Range("A1:F1").Style.Font.FontColor =
            XLColor.White;

        transactionSheet.Range("A1:F1").Style.Font.Bold = true;

        transactionSheet.Range("A1:F1").Style.Font.FontSize = 16;

        transactionSheet.Cell("A2").Value =
            $"Period: {selectedStartDate:dd MMM yyyy} - {selectedEndDate:dd MMM yyyy}";

        transactionSheet.Range("A2:F2").Merge();

        // Headers
        var headers = new[]
        {
        "Date",
        "Type",
        "Category",
        "Amount",
        "Description",
        "Created At"
    };

        for (var i = 0; i < headers.Length; i++)
        {
            transactionSheet.Cell(4, i + 1).Value =
                headers[i];
        }

        transactionSheet.Range("A4:F4").Style.Fill.BackgroundColor = accentColor;

        transactionSheet.Range("A4:F4").Style.Font.FontColor =
            XLColor.White;

        transactionSheet.Range("A4:F4").Style.Font.Bold = true;

        // Data
        var transactionRow = 5;

        foreach (var transaction in transactions)
        {
            transactionSheet.Cell(transactionRow, 1).Value =
                transaction.TransactionDate;

            transactionSheet.Cell(transactionRow, 2).Value =
                transaction.Type.ToString();

            transactionSheet.Cell(transactionRow, 3).Value =
                transaction.Category?.Name ?? "-";

            transactionSheet.Cell(transactionRow, 4).Value =
                transaction.Amount;

            transactionSheet.Cell(transactionRow, 5).Value =
                transaction.Description ?? "-";

            transactionSheet.Cell(transactionRow, 6).Value =
                transaction.CreatedAt;

            transactionRow++;
        }

        // Formatting
        transactionSheet.Column(1)
            .Style.DateFormat.Format =
            "dd MMM yyyy";

        transactionSheet.Column(4)
            .Style.NumberFormat.Format =
            "₹#,##0.00";

        transactionSheet.Column(6)
            .Style.DateFormat.Format =
            "dd MMM yyyy HH:mm";

        // Add Excel table if transactions exist
        if (transactions.Any())
        {
            var transactionTable =
                transactionSheet.Range(
                    4,
                    1,
                    transactionRow - 1,
                    6)
                .CreateTable();

            transactionTable.Name =
                "TransactionsTable";

            transactionTable.Theme =
                XLTableTheme.TableStyleMedium2;
        }

        transactionSheet.SheetView.FreezeRows(4);

        transactionSheet.Column(1).Width = 15;
        transactionSheet.Column(2).Width = 14;
        transactionSheet.Column(3).Width = 20;
        transactionSheet.Column(4).Width = 16;
        transactionSheet.Column(5).Width = 35;
        transactionSheet.Column(6).Width = 22;

        // =========================================================
        // SHEET 3: CATEGORIES
        // =========================================================

        var categorySheet =
            workbook.Worksheets.Add("Categories");


        categorySheet.Cell("A1").Value =
            "Expense by Category";

        categorySheet.Range("A1:C1").Merge();

        categorySheet.Range("A1:C1").Style.Fill.BackgroundColor = headerColor;

        categorySheet.Range("A1:C1").Style.Font.FontColor =
            XLColor.White;

        categorySheet.Range("A1:C1").Style.Font.Bold = true;

        categorySheet.Range("A1:C1").Style.Font.FontSize = 16;

        categorySheet.Cell("A3").Value = "Category";
        categorySheet.Cell("B3").Value = "Amount";
        categorySheet.Cell("C3").Value = "Percentage";

        categorySheet.Range("A3:C3").Style.Fill.BackgroundColor = accentColor;

        categorySheet.Range("A3:C3").Style.Font.FontColor =
            XLColor.White;

        categorySheet.Range("A3:C3").Style.Font.Bold = true;

        var categoryRow = 4;

        foreach (var category in categoryData)
        {
            var percentage = totalExpense > 0
                ? category.Amount / totalExpense
                : 0;

            categorySheet.Cell(categoryRow, 1).Value =
                category.Name;

            categorySheet.Cell(categoryRow, 2).Value =
                category.Amount;

            categorySheet.Cell(categoryRow, 3).Value =
                percentage;

            categoryRow++;
        }

        categorySheet.Column(2)
            .Style.NumberFormat.Format =
            "₹#,##0.00";

        categorySheet.Column(3)
            .Style.NumberFormat.Format =
            "0.00%";

        if (categoryData.Any())
        {
            var categoryTable =
                categorySheet.Range(
                    3,
                    1,
                    categoryRow - 1,
                    3)
                .CreateTable();

            categoryTable.Name =
                "CategoryTable";

            categoryTable.Theme =
                XLTableTheme.TableStyleMedium4;
        }

        categorySheet.SheetView.FreezeRows(3);

        categorySheet.Column(1).Width = 25;
        categorySheet.Column(2).Width = 18;
        categorySheet.Column(3).Width = 18;

        // =========================================================
        // SHEET 4: MONTHLY SUMMARY
        // =========================================================

        var monthlySheet =
            workbook.Worksheets.Add("Monthly Summary");


        monthlySheet.Cell("A1").Value =
            "Monthly Financial Summary";

        monthlySheet.Range("A1:D1").Merge();

        monthlySheet.Range("A1:D1").Style.Fill.BackgroundColor = headerColor;

        monthlySheet.Range("A1:D1").Style.Font.FontColor =
            XLColor.White;

        monthlySheet.Range("A1:D1").Style.Font.Bold = true;

        monthlySheet.Range("A1:D1").Style.Font.FontSize = 16;

        monthlySheet.Cell("A3").Value = "Month";
        monthlySheet.Cell("B3").Value = "Income";
        monthlySheet.Cell("C3").Value = "Expense";
        monthlySheet.Cell("D3").Value = "Balance";

        monthlySheet.Range("A3:D3").Style.Fill.BackgroundColor = accentColor;

        monthlySheet.Range("A3:D3").Style.Font.FontColor =
            XLColor.White;

        monthlySheet.Range("A3:D3").Style.Font.Bold = true;

        var monthlyRow = 4;

        foreach (var month in monthlyData)
        {
            monthlySheet.Cell(monthlyRow, 1).Value =
                month.Month;

            monthlySheet.Cell(monthlyRow, 2).Value =
                month.Income;

            monthlySheet.Cell(monthlyRow, 3).Value =
                month.Expense;

            monthlySheet.Cell(monthlyRow, 4).Value =
                month.Balance;

            monthlyRow++;
        }

        monthlySheet.Range(
                4,
                2,
                Math.Max(monthlyRow - 1, 4),
                4)
            .Style.NumberFormat.Format =
            "₹#,##0.00";

        if (monthlyData.Any())
        {
            var monthlyTable =
                monthlySheet.Range(
                    3,
                    1,
                    monthlyRow - 1,
                    4)
                .CreateTable();

            monthlyTable.Name =
                "MonthlyTable";

            monthlyTable.Theme =
                XLTableTheme.TableStyleMedium2;
        }

        monthlySheet.SheetView.FreezeRows(3);

        monthlySheet.Column(1).Width = 18;
        monthlySheet.Column(2).Width = 18;
        monthlySheet.Column(3).Width = 18;
        monthlySheet.Column(4).Width = 18;

        // =========================================================
        // SAVE FILE
        // =========================================================

        using var stream = new MemoryStream();

        workbook.SaveAs(stream);

        stream.Position = 0;

        var fileName =
            $"FinTrack_Report_{selectedStartDate:yyyyMMdd}_{selectedEndDate:yyyyMMdd}.xlsx";

        return File(
            stream.ToArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            fileName);
    }


    // =========================================================
    // PDF EXPORT
    // =========================================================

    [HttpGet]
    public async Task<IActionResult> ExportPdf(
    DateTime? startDate,
    DateTime? endDate)
    {
        var userId = _userManager.GetUserId(User);

        if (userId == null)
        {
            return Challenge();
        }

        var (selectedStartDate, selectedEndDate) =
            GetReportDateRange(startDate, endDate);

        var queryEndDate = selectedEndDate.AddDays(1);

        // GET TRANSACTIONS
        var transactions = await _context.Transactions
            .AsNoTracking()
            .Include(t => t.Category)
            .Where(t =>
                t.UserId == userId &&
                t.TransactionDate >= selectedStartDate &&
                t.TransactionDate < queryEndDate)
            .OrderBy(t => t.TransactionDate)
            .ToListAsync();

        // SUMMARY
        var totalIncome = transactions
            .Where(t => t.Type == TransactionType.Income)
            .Sum(t => t.Amount);

        var totalExpense = transactions
            .Where(t => t.Type == TransactionType.Expense)
            .Sum(t => t.Amount);

        var balance = totalIncome - totalExpense;

        var savingsRate = totalIncome > 0
            ? (balance / totalIncome) * 100
            : 0;

        // CATEGORY DATA
        var categoryData = transactions
            .Where(t =>
                t.Type == TransactionType.Expense &&
                t.Category != null)
            .GroupBy(t => t.Category!.Name)
            .Select(g => new
            {
                Name = g.Key,
                Amount = g.Sum(t => t.Amount)
            })
            .OrderByDescending(x => x.Amount)
            .ToList();

        // MONTHLY DATA
        var monthlyData = transactions
            .GroupBy(t => new
            {
                t.TransactionDate.Year,
                t.TransactionDate.Month
            })
            .OrderBy(g => g.Key.Year)
            .ThenBy(g => g.Key.Month)
            .Select(g =>
            {
                var income = g
                    .Where(t => t.Type == TransactionType.Income)
                    .Sum(t => t.Amount);

                var expense = g
                    .Where(t => t.Type == TransactionType.Expense)
                    .Sum(t => t.Amount);

                return new
                {
                    Month = new DateTime(
                        g.Key.Year,
                        g.Key.Month,
                        1),

                    Income = income,
                    Expense = expense,
                    Balance = income - expense
                };
            })
            .ToList();

        var highestExpenseCategory =
            categoryData.FirstOrDefault();

        // PDF
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(35);
                page.DefaultTextStyle(x =>
                    x.FontSize(9));

                // HEADER
                page.Header()
                    .Column(column =>
                    {
                        column.Spacing(3);

                        column.Item()
                            .Text("FinTrack")
                            .FontSize(24)
                            .Bold()
                            .FontColor(Colors.Blue.Darken2);

                        column.Item()
                            .Text("Financial Report")
                            .FontSize(15)
                            .Bold();

                        column.Item()
                            .Text(
                                $"Period: {selectedStartDate:dd MMM yyyy} - {selectedEndDate:dd MMM yyyy}")
                            .FontSize(9)
                            .FontColor(Colors.Grey.Darken1);

                        column.Item()
                            .PaddingTop(8)
                            .LineHorizontal(1)
                            .LineColor(Colors.Grey.Lighten2);
                    });

                // FOOTER
                page.Footer()
                    .AlignCenter()
                    .Text(text =>
                    {
                        text.Span("FinTrack • Personal Finance  |  ");

                        text.CurrentPageNumber();

                        text.Span(" / ");

                        text.TotalPages();
                    });

                // CONTENT
                page.Content()
                    .PaddingTop(15)
                    .Column(column =>
                    {
                        column.Spacing(14);

                        // SUMMARY TITLE
                        column.Item()
                            .Text("Financial Summary")
                            .FontSize(13)
                            .Bold();

                        // SUMMARY CARDS
                        column.Item()
                            .Grid(grid =>
                            {
                                grid.Columns(4);
                                grid.Spacing(8);

                                // Income
                                grid.Item()
                                    .Background(Colors.Green.Lighten5)
                                    .Border(1)
                                    .BorderColor(Colors.Green.Lighten2)
                                    .Padding(10)
                                    .Column(card =>
                                    {
                                        card.Spacing(4);

                                        card.Item()
                                            .Text("TOTAL INCOME")
                                            .FontSize(7)
                                            .Bold()
                                            .FontColor(
                                                Colors.Grey.Darken1);

                                        card.Item()
                                            .Text(
                                                $"₹{totalIncome:N2}")
                                            .FontSize(13)
                                            .Bold()
                                            .FontColor(
                                                Colors.Green.Darken2);
                                    });

                                // Expense
                                grid.Item()
                                    .Background(Colors.Red.Lighten5)
                                    .Border(1)
                                    .BorderColor(Colors.Red.Lighten2)
                                    .Padding(10)
                                    .Column(card =>
                                    {
                                        card.Spacing(4);

                                        card.Item()
                                            .Text("TOTAL EXPENSE")
                                            .FontSize(7)
                                            .Bold()
                                            .FontColor(
                                                Colors.Grey.Darken1);

                                        card.Item()
                                            .Text(
                                                $"₹{totalExpense:N2}")
                                            .FontSize(13)
                                            .Bold()
                                            .FontColor(
                                                Colors.Red.Darken2);
                                    });

                                // Balance
                                grid.Item()
                                    .Background(Colors.Blue.Lighten5)
                                    .Border(1)
                                    .BorderColor(Colors.Blue.Lighten2)
                                    .Padding(10)
                                    .Column(card =>
                                    {
                                        card.Spacing(4);

                                        card.Item()
                                            .Text("NET BALANCE")
                                            .FontSize(7)
                                            .Bold()
                                            .FontColor(
                                                Colors.Grey.Darken1);

                                        card.Item()
                                            .Text(
                                                $"₹{balance:N2}")
                                            .FontSize(13)
                                            .Bold()
                                            .FontColor(
                                                Colors.Blue.Darken2);
                                    });

                                // Savings
                                grid.Item()
                                    .Background(Colors.Grey.Lighten4)
                                    .Border(1)
                                    .BorderColor(Colors.Grey.Lighten2)
                                    .Padding(10)
                                    .Column(card =>
                                    {
                                        card.Spacing(4);

                                        card.Item()
                                            .Text("SAVINGS RATE")
                                            .FontSize(7)
                                            .Bold()
                                            .FontColor(
                                                Colors.Grey.Darken1);

                                        card.Item()
                                            .Text(
                                                $"{savingsRate:N2}%")
                                            .FontSize(13)
                                            .Bold()
                                            .FontColor(
                                                Colors.Blue.Darken2);
                                    });
                            });

                        // ADDITIONAL SUMMARY
                        column.Item()
                            .Grid(grid =>
                            {
                                grid.Columns(2);
                                grid.Spacing(10);

                                grid.Item()
                                    .Background(Colors.Grey.Lighten5)
                                    .Padding(10)
                                    .Column(card =>
                                    {
                                        card.Item()
                                            .Text("TRANSACTIONS")
                                            .FontSize(8)
                                            .Bold();

                                        card.Item()
                                            .PaddingTop(3)
                                            .Text(
                                                transactions.Count
                                                    .ToString())
                                            .FontSize(14)
                                            .Bold();
                                    });

                                grid.Item()
                                    .Background(Colors.Grey.Lighten5)
                                    .Padding(10)
                                    .Column(card =>
                                    {
                                        card.Item()
                                            .Text(
                                                "HIGHEST EXPENSE CATEGORY")
                                            .FontSize(8)
                                            .Bold();

                                        card.Item()
                                            .PaddingTop(3)
                                            .Text(
                                                highestExpenseCategory
                                                    ?.Name
                                                ?? "No expenses")
                                            .FontSize(12)
                                            .Bold();

                                        if (highestExpenseCategory != null)
                                        {
                                            card.Item()
                                                .Text(
                                                    $"₹{highestExpenseCategory.Amount:N2}")
                                                .FontSize(9)
                                                .FontColor(
                                                    Colors.Red.Darken1);
                                        }
                                    });
                            });

                        // CATEGORY BREAKDOWN
                        column.Item()
                            .Text("Expense by Category")
                            .FontSize(13)
                            .Bold();

                        if (categoryData.Any())
                        {
                            column.Item()
                                .Table(table =>
                                {
                                    table.ColumnsDefinition(columns =>
                                    {
                                        columns.RelativeColumn(2.5f);
                                        columns.RelativeColumn(1.5f);
                                        columns.RelativeColumn(1f);
                                    });

                                    table.Header(header =>
                                    {
                                        header.Cell()
                                            .Background(
                                                Colors.Blue.Darken2)
                                            .Padding(7)
                                            .Text("Category")
                                            .FontColor(
                                                Colors.White)
                                            .Bold();

                                        header.Cell()
                                            .Background(
                                                Colors.Blue.Darken2)
                                            .Padding(7)
                                            .AlignRight()
                                            .Text("Amount")
                                            .FontColor(
                                                Colors.White)
                                            .Bold();

                                        header.Cell()
                                            .Background(
                                                Colors.Blue.Darken2)
                                            .Padding(7)
                                            .AlignRight()
                                            .Text("Percentage")
                                            .FontColor(
                                                Colors.White)
                                            .Bold();
                                    });

                                    foreach (var category in categoryData)
                                    {
                                        var percentage =
                                            totalExpense > 0
                                                ? (category.Amount /
                                                    totalExpense) * 100
                                                : 0;

                                        table.Cell()
                                            .BorderBottom(1)
                                            .BorderColor(
                                                Colors.Grey.Lighten2)
                                            .Padding(6)
                                            .Text(category.Name);

                                        table.Cell()
                                            .BorderBottom(1)
                                            .BorderColor(
                                                Colors.Grey.Lighten2)
                                            .Padding(6)
                                            .AlignRight()
                                            .Text(
                                                $"₹{category.Amount:N2}");

                                        table.Cell()
                                            .BorderBottom(1)
                                            .BorderColor(
                                                Colors.Grey.Lighten2)
                                            .Padding(6)
                                            .AlignRight()
                                            .Text(
                                                $"{percentage:N2}%");
                                    }
                                });
                        }
                        else
                        {
                            column.Item()
                                .Text("No expense data available.")
                                .FontColor(
                                    Colors.Grey.Darken1);
                        }

                        // MONTHLY SUMMARY
                        column.Item()
                            .Text("Monthly Financial Summary")
                            .FontSize(13)
                            .Bold();

                        if (monthlyData.Any())
                        {
                            column.Item()
                                .Table(table =>
                                {
                                    table.ColumnsDefinition(columns =>
                                    {
                                        columns.RelativeColumn(1.5f);
                                        columns.RelativeColumn(1.5f);
                                        columns.RelativeColumn(1.5f);
                                        columns.RelativeColumn(1.5f);
                                    });

                                    table.Header(header =>
                                    {
                                        header.Cell()
                                            .Background(
                                                Colors.Blue.Darken2)
                                            .Padding(7)
                                            .Text("Month")
                                            .FontColor(
                                                Colors.White)
                                            .Bold();

                                        header.Cell()
                                            .Background(
                                                Colors.Blue.Darken2)
                                            .Padding(7)
                                            .AlignRight()
                                            .Text("Income")
                                            .FontColor(
                                                Colors.White)
                                            .Bold();

                                        header.Cell()
                                            .Background(
                                                Colors.Blue.Darken2)
                                            .Padding(7)
                                            .AlignRight()
                                            .Text("Expense")
                                            .FontColor(
                                                Colors.White)
                                            .Bold();

                                        header.Cell()
                                            .Background(
                                                Colors.Blue.Darken2)
                                            .Padding(7)
                                            .AlignRight()
                                            .Text("Balance")
                                            .FontColor(
                                                Colors.White)
                                            .Bold();
                                    });

                                    foreach (var month in monthlyData)
                                    {
                                        table.Cell()
                                            .BorderBottom(1)
                                            .BorderColor(
                                                Colors.Grey.Lighten2)
                                            .Padding(6)
                                            .Text(
                                                month.Month
                                                    .ToString("MMM yyyy"));

                                        table.Cell()
                                            .BorderBottom(1)
                                            .BorderColor(
                                                Colors.Grey.Lighten2)
                                            .Padding(6)
                                            .AlignRight()
                                            .Text(
                                                $"₹{month.Income:N2}");

                                        table.Cell()
                                            .BorderBottom(1)
                                            .BorderColor(
                                                Colors.Grey.Lighten2)
                                            .Padding(6)
                                            .AlignRight()
                                            .Text(
                                                $"₹{month.Expense:N2}");

                                        table.Cell()
                                            .BorderBottom(1)
                                            .BorderColor(
                                                Colors.Grey.Lighten2)
                                            .Padding(6)
                                            .AlignRight()
                                            .Text(
                                                $"₹{month.Balance:N2}");
                                    }
                                });
                        }
                        else
                        {
                            column.Item()
                                .Text("No monthly data available.")
                                .FontColor(
                                    Colors.Grey.Darken1);
                        }

                        // TRANSACTIONS
                        column.Item()
                            .Text("Transaction Details")
                            .FontSize(13)
                            .Bold();

                        if (transactions.Any())
                        {
                            column.Item()
                                .Table(table =>
                                {
                                    table.ColumnsDefinition(columns =>
                                    {
                                        columns.RelativeColumn(1.3f);
                                        columns.RelativeColumn(1f);
                                        columns.RelativeColumn(1.5f);
                                        columns.RelativeColumn(1.4f);
                                    });

                                    table.Header(header =>
                                    {
                                        header.Cell()
                                            .Background(
                                                Colors.Blue.Darken2)
                                            .Padding(6)
                                            .Text("Date")
                                            .FontColor(
                                                Colors.White)
                                            .Bold();

                                        header.Cell()
                                            .Background(
                                                Colors.Blue.Darken2)
                                            .Padding(6)
                                            .Text("Type")
                                            .FontColor(
                                                Colors.White)
                                            .Bold();

                                        header.Cell()
                                            .Background(
                                                Colors.Blue.Darken2)
                                            .Padding(6)
                                            .Text("Category")
                                            .FontColor(
                                                Colors.White)
                                            .Bold();

                                        header.Cell()
                                            .Background(
                                                Colors.Blue.Darken2)
                                            .Padding(6)
                                            .AlignRight()
                                            .Text("Amount")
                                            .FontColor(
                                                Colors.White)
                                            .Bold();
                                    });

                                    foreach (var transaction in transactions)
                                    {
                                        table.Cell()
                                            .BorderBottom(1)
                                            .BorderColor(
                                                Colors.Grey.Lighten2)
                                            .Padding(5)
                                            .Text(
                                                transaction
                                                    .TransactionDate
                                                    .ToString(
                                                        "dd MMM yyyy"));

                                        table.Cell()
                                            .BorderBottom(1)
                                            .BorderColor(
                                                Colors.Grey.Lighten2)
                                            .Padding(5)
                                            .Text(
                                                transaction.Type
                                                    .ToString());

                                        table.Cell()
                                            .BorderBottom(1)
                                            .BorderColor(
                                                Colors.Grey.Lighten2)
                                            .Padding(5)
                                            .Text(
                                                transaction.Category
                                                    ?.Name ?? "-");

                                        table.Cell()
                                            .BorderBottom(1)
                                            .BorderColor(
                                                Colors.Grey.Lighten2)
                                            .Padding(5)
                                            .AlignRight()
                                            .Text(
                                                $"₹{transaction.Amount:N2}");
                                    }
                                });
                        }
                        else
                        {
                            column.Item()
                                .Text("No transactions found.")
                                .FontColor(
                                    Colors.Grey.Darken1);
                        }
                    });
            });
        });

        var pdfBytes = document.GeneratePdf();

        var fileName =
            $"FinTrack_Report_{selectedStartDate:yyyyMMdd}_{selectedEndDate:yyyyMMdd}.pdf";

        return File(
            pdfBytes,
            "application/pdf",
            fileName);
    }


    // =========================================================
    // DATE RANGE HELPER
    // =========================================================

    private static (DateTime StartDate, DateTime EndDate)
        GetReportDateRange(
            DateTime? startDate,
            DateTime? endDate)
    {
        var selectedStartDate = startDate ??
            new DateTime(
                DateTime.Today.Year,
                DateTime.Today.Month,
                1);

        var selectedEndDate = endDate ??
            selectedStartDate.AddMonths(1).AddDays(-1);

        selectedStartDate = selectedStartDate.Date;
        selectedEndDate = selectedEndDate.Date;

        if (selectedEndDate < selectedStartDate)
        {
            selectedEndDate = selectedStartDate;
        }

        return (
            selectedStartDate,
            selectedEndDate
        );
    }
}