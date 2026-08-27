using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace personal_finance_Tracker.Models;

public class Budget
{
    public int Id { get; set; }

    // User who owns this budget
    [Required]
    public string UserId { get; set; } = string.Empty;

    // Category for this budget
    [Required]
    public int CategoryId { get; set; }

    // Maximum amount the user wants to spend
    [Required]
    [Range(
        0.01,
        999999999.99,
        ErrorMessage = "Budget amount must be greater than 0.")]
    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }

    // First day of the budget month
    [Required]
    public DateTime Month { get; set; }

    // Relationships
    public ApplicationUser User { get; set; } = null!;

    public Category Category { get; set; } = null!;
}