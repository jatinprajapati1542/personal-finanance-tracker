using personal_finance_Tracker.Models;

namespace personal_finance_Tracker.ViewModels;

public class TransactionViewModel
{
    public List<Transaction> Transactions { get; set; } = new();

    public List<Category> Categories { get; set; } = new();


    // Filters

    public TransactionType? Type { get; set; }

    public int? CategoryId { get; set; }

    public DateTime? StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    public string? Search { get; set; }


    // Pagination

    public int CurrentPage { get; set; } = 1;

    public int TotalPages { get; set; }


    // Summary

    public decimal TotalIncome { get; set; }

    public decimal TotalExpense { get; set; }

    public decimal Balance { get; set; }

    public int TotalTransactions { get; set; }
}