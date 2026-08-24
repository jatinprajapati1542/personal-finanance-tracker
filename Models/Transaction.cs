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

// using System.ComponentModel.DataAnnotations;
// using System.ComponentModel.DataAnnotations.Schema;

// namespace personal_finance_Tracker.Models;

// public class Transaction
// {
//     public int Id { get; set; }


//     // User who owns this transaction
//     public string UserId { get; set; } = string.Empty;


//     // Category
//     [Required(ErrorMessage = "Please select a category.")]
//     [Display(Name = "Category")]
//     public int CategoryId { get; set; }


//     // Amount
//     [Required(ErrorMessage = "Please enter an amount.")]
//     [Range(
//         0.01,
//         999999999.99,
//         ErrorMessage = "Amount must be greater than 0.")]
//     [Column(TypeName = "decimal(18,2)")]
//     public decimal Amount { get; set; }


//     // Income / Expense
//     public TransactionType Type { get; set; }


//     // Description
//     [StringLength(
//         500,
//         ErrorMessage = "Description cannot exceed 500 characters.")]
//     public string? Description { get; set; }


//     // Transaction date
//     [Required(ErrorMessage = "Please select a date.")]
//     [Display(Name = "Transaction Date")]
//     public DateTime TransactionDate { get; set; }


//     // Created date
//     public DateTime CreatedAt { get; set; }


//     // Navigation properties
//     // These are NOT required because they are loaded by EF Core.

//     public ApplicationUser? User { get; set; }

//     public Category? Category { get; set; }
// }