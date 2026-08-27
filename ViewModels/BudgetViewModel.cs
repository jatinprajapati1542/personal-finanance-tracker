namespace personal_finance_Tracker.ViewModels;

public class BudgetViewModel
{
    public int Id { get; set; }

    public string CategoryName { get; set; } = string.Empty;

    public DateTime Month { get; set; }

    public decimal Amount { get; set; }

    public decimal Spent { get; set; }

    public decimal Remaining { get; set; }

    public decimal Percentage { get; set; }
}