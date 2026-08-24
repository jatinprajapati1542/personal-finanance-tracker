// using personal_finance_Tracker.Models;

// namespace personal_finance_Tracker.ViewModels;

// public class DashboardViewModel
// {
//     public decimal TotalIncome { get; set; }

//     public decimal TotalExpense { get; set; }

//     public decimal Balance => TotalIncome - TotalExpense;

//     public int TotalTransactions { get; set; }

//     public List<Transaction> RecentTransactions { get; set; }
//         = new List<Transaction>();

//     public List<CategorySummaryViewModel> CategorySummary { get; set; }
//         = new List<CategorySummaryViewModel>();
// }

// public class CategorySummaryViewModel
// {
//     public string CategoryName { get; set; } = string.Empty;

//     public decimal Amount { get; set; }

//     public string Type { get; set; } = string.Empty;
// }


using personal_finance_Tracker.Models;

namespace personal_finance_Tracker.ViewModels;

public class DashboardViewModel
{
    public decimal TotalIncome { get; set; }

    public decimal TotalExpense { get; set; }

    public decimal Balance { get; set; }

    public int TotalTransactions { get; set; }

    public List<Transaction> RecentTransactions { get; set; }
        = new();


    // Data for Income vs Expense chart

    public decimal ChartIncome { get; set; }

    public decimal ChartExpense { get; set; }
}