using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace personal_finance_Tracker.ViewModels;

public class ProfileViewModel
{
    [Display(Name = "Full Name")]
    public string? FullName { get; set; }

    [Display(Name = "Email")]
    public string? Email { get; set; }

    [Display(Name = "Username")]
    public string? UserName { get; set; }

    public string? ProfileImage { get; set; }


    // Used when editing the profile

    [Required(ErrorMessage = "Full name is required.")]
    [StringLength(
        100,
        MinimumLength = 2,
        ErrorMessage = "Full name must be between 2 and 100 characters.")]
    [Display(Name = "Full Name")]
    public string EditFullName { get; set; } = string.Empty;


    [Display(Name = "Profile Image")]
    public IFormFile? ProfileImageFile { get; set; }
}