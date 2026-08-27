
using personal_finance_Tracker.Models;

namespace personal_finance_Tracker.ViewModels;

public class DashboardViewModel
{
    // ==========================================
    // Summary
    // ==========================================

    public decimal TotalIncome { get; set; }

    public decimal TotalExpense { get; set; }

    public decimal Balance { get; set; }

    public int TotalTransactions { get; set; }


    // ==========================================
    // Budget Summary
    // ==========================================

    public decimal TotalBudget { get; set; }

    public decimal TotalBudgetSpent { get; set; }

    public decimal TotalBudgetRemaining { get; set; }


    // ==========================================
    // Recent Transactions
    // ==========================================

    public List<Transaction> RecentTransactions { get; set; }
        = new();


    // ==========================================
    // Income vs Expense Chart
    // ==========================================

    public decimal ChartIncome { get; set; }

    public decimal ChartExpense { get; set; }


    // ==========================================
    // Monthly Chart
    // ==========================================

    public List<string> ChartMonths { get; set; }
        = new();

    public List<decimal> MonthlyIncome { get; set; }
        = new();

    public List<decimal> MonthlyExpense { get; set; }
        = new();


    // ==========================================
    // Budget Overview
    // ==========================================

    public List<DashboardBudgetViewModel> Budgets { get; set; }
        = new();
}


// ==========================================
// Dashboard Budget Item
// ==========================================

public class DashboardBudgetViewModel
{
    public string CategoryName { get; set; } = string.Empty;

    public decimal BudgetAmount { get; set; }

    public decimal SpentAmount { get; set; }

    public decimal RemainingAmount { get; set; }

    public decimal Percentage { get; set; }

    // Budget status
    public string Status
    {
        get
        {
            if (Percentage >= 100)
                return "Exceeded";

            if (Percentage >= 80)
                return "Warning";

            return "Healthy";
        }
    }

    // Bootstrap progress-bar class
    public string ProgressClass
    {
        get
        {
            if (Percentage >= 100)
                return "bg-danger";

            if (Percentage >= 80)
                return "bg-warning";

            return "bg-success";
        }
    }
}