using System.ComponentModel.DataAnnotations;

namespace personal_finance_Tracker.Models;

public class Category
{
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    public TransactionType Type { get; set; }

    // User who owns this category
    [Required]
    public string UserId { get; set; } = string.Empty;

    public ApplicationUser User { get; set; } = null!;

    // One category can have many transactions
    public ICollection<Transaction> Transactions { get; set; }
        = new List<Transaction>();
}