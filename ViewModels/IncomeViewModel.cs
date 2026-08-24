using System.ComponentModel.DataAnnotations;

namespace personal_finance_Tracker.ViewModels;

public class IncomeViewModel
{
    [Required]
    [Range(0.01, 999999999)]
    [Display(Name = "Amount")]
    public decimal Amount { get; set; }

    [Required]
    [Display(Name = "Category")]
    public int CategoryId { get; set; }

    [StringLength(500)]
    [Display(Name = "Description")]
    public string? Description { get; set; }

    [Required]
    [DataType(DataType.Date)]
    [Display(Name = "Date")]
    public DateTime TransactionDate { get; set; } = DateTime.Today;
}