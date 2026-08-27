using Microsoft.AspNetCore.Identity;

namespace personal_finance_Tracker.Models;

public class ApplicationUser : IdentityUser
{
    public string FullName { get; set; } = string.Empty;

    public string? ProfileImage { get; set; }

    public ICollection<Category> Categories { get; set; }
        = new List<Category>();

    public ICollection<Transaction> Transactions { get; set; }
        = new List<Transaction>();

    public ICollection<Budget> Budgets { get; set; }
        = new List<Budget>();
}