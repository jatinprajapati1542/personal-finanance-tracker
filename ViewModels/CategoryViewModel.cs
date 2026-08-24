using System.ComponentModel.DataAnnotations;
using personal_finance_Tracker.Models;

namespace personal_finance_Tracker.ViewModels;

public class CategoryViewModel
{
    [Required]
    [StringLength(100)]
    [Display(Name = "Category Name")]
    public string Name { get; set; } = string.Empty;

    public TransactionType Type { get; set; }
}