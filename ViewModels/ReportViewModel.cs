// using personal_finance_Tracker.Models;

// namespace personal_finance_Tracker.ViewModels;

// public class ReportViewModel
// {

//     public decimal ChartIncome { get; set; }

//     public decimal ChartExpense { get; set; }

//     // ==========================================
//     // Selected Date Range
//     // ==========================================

//     public DateTime StartDate { get; set; }

//     public DateTime EndDate { get; set; }


//     // ==========================================
//     // Summary
//     // ==========================================

//     public decimal TotalIncome { get; set; }

//     public decimal TotalExpense { get; set; }

//     public decimal Balance { get; set; }

//     public int TotalTransactions { get; set; }


//     // ==========================================
//     // Category-wise Expense
//     // ==========================================

//     public List<CategoryExpenseViewModel> CategoryExpenses { get; set; }
//         = new();


//     // ==========================================
//     // Monthly Summary
//     // ==========================================

//     public List<MonthlyReportViewModel> MonthlyData { get; set; }
//         = new();
// }


// // ==========================================
// // Category Expense
// // ==========================================

// public class CategoryExpenseViewModel
// {
//     public string CategoryName { get; set; } = string.Empty;

//     public decimal Amount { get; set; }

//     public decimal Percentage { get; set; }
// }


// // ==========================================
// // Monthly Report
// // ==========================================

// public class MonthlyReportViewModel
// {
//     public string Month { get; set; } = string.Empty;

//     public decimal Income { get; set; }

//     public decimal Expense { get; set; }

//     public decimal Balance { get; set; }
// }

using personal_finance_Tracker.Models;

namespace personal_finance_Tracker.ViewModels;

public class ReportViewModel
{
    // Report Period
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }

    // Main Summary
    public decimal TotalIncome { get; set; }
    public decimal TotalExpense { get; set; }
    public decimal Balance { get; set; }
    public int TotalTransactions { get; set; }

    // Financial Analysis
    public decimal SavingsRate { get; set; }

    public string HighestExpenseCategory { get; set; }
        = "No expenses";

    public decimal HighestExpenseAmount { get; set; }

    // Chart Data
    public decimal ChartIncome { get; set; }
    public decimal ChartExpense { get; set; }

    // Expense by Category
    public List<CategoryExpenseViewModel> CategoryExpenses { get; set; }
        = new();

    // Monthly Income / Expense Trend
    public List<MonthlyReportViewModel> MonthlyData { get; set; }
        = new();
}


/// <summary>
/// Represents expense information grouped by category.
/// </summary>
public class CategoryExpenseViewModel
{
    public string CategoryName { get; set; } = string.Empty;

    public decimal Amount { get; set; }

    public decimal Percentage { get; set; }
}


/// <summary>
/// Represents income and expense information for a month.
/// </summary>
public class MonthlyReportViewModel
{
    public string Month { get; set; } = string.Empty;

    public decimal Income { get; set; }

    public decimal Expense { get; set; }

    public decimal Balance { get; set; }
}