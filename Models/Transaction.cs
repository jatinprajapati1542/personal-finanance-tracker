using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace personal_finance_Tracker.Models;

public class Transaction
{
    public int Id { get; set; }

    // [Required]
    public string UserId { get; set; } = string.Empty;

    [Required]
    public int CategoryId { get; set; }

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }

    [Required]
    public TransactionType Type { get; set; }

    [StringLength(500)]
    public string? Description { get; set; }

    [Required]
    public DateTime TransactionDate { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Relationships

    public ApplicationUser? User { get; set; } = null!;

    public Category? Category { get; set; } = null!;
}