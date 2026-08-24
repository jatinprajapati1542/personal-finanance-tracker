using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using personal_finance_Tracker.Models;
using personal_finance_Tracker.ViewModels;

namespace personal_finance_Tracker.Controllers;

[Authorize]
public class ProfileController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IWebHostEnvironment _environment;

    public ProfileController(
        UserManager<ApplicationUser> userManager,
        IWebHostEnvironment environment)
    {
        _userManager = userManager;
        _environment = environment;
    }


    // GET: /Profile
    public async Task<IActionResult> Index()
    {
        var user = await _userManager.GetUserAsync(User);

        if (user == null)
        {
            return Challenge();
        }


        var viewModel = new ProfileViewModel
        {
            FullName = user.FullName,
            Email = user.Email,
            UserName = user.UserName,
            ProfileImage = user.ProfileImage
        };


        return View(viewModel);
    }


    // GET: /Profile/Edit
    [HttpGet]
    public async Task<IActionResult> Edit()
    {
        var user = await _userManager.GetUserAsync(User);

        if (user == null)
        {
            return Challenge();
        }


        var viewModel = new ProfileViewModel
        {
            FullName = user.FullName,
            Email = user.Email,
            UserName = user.UserName,
            ProfileImage = user.ProfileImage,
            EditFullName = user.FullName
        };


        return View(viewModel);
    }


    // POST: /Profile/Edit
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(ProfileViewModel model)
    {
        var user = await _userManager.GetUserAsync(User);

        if (user == null)
        {
            return Challenge();
        }


        // Validate uploaded image

        if (model.ProfileImageFile != null)
        {
            var allowedExtensions = new[]
            {
                ".jpg",
                ".jpeg",
                ".png",
                ".webp"
            };

            var extension =
                Path.GetExtension(model.ProfileImageFile.FileName)
                    .ToLowerInvariant();


            if (!allowedExtensions.Contains(extension))
            {
                ModelState.AddModelError(
                    nameof(model.ProfileImageFile),
                    "Only JPG, JPEG, PNG and WEBP images are allowed.");
            }


            // 5 MB maximum

            if (model.ProfileImageFile.Length > 5 * 1024 * 1024)
            {
                ModelState.AddModelError(
                    nameof(model.ProfileImageFile),
                    "Profile image must be smaller than 5 MB.");
            }
        }


        if (!ModelState.IsValid)
        {
            model.FullName = user.FullName;
            model.Email = user.Email;
            model.UserName = user.UserName;
            model.ProfileImage = user.ProfileImage;

            return View(model);
        }


        // Update Full Name

        user.FullName = model.EditFullName.Trim();


        // Upload Profile Image

        if (model.ProfileImageFile != null)
        {
            var uploadsFolder = Path.Combine(
                _environment.WebRootPath,
                "images",
                "profiles");


            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }


            // Delete old image

            if (!string.IsNullOrWhiteSpace(user.ProfileImage))
            {
                var oldImagePath = Path.Combine(
                    uploadsFolder,
                    user.ProfileImage);

                if (System.IO.File.Exists(oldImagePath))
                {
                    System.IO.File.Delete(oldImagePath);
                }
            }


            // Create unique file name

            var extension =
                Path.GetExtension(
                    model.ProfileImageFile.FileName)
                .ToLowerInvariant();


            var fileName =
                $"{Guid.NewGuid()}{extension}";


            var filePath = Path.Combine(
                uploadsFolder,
                fileName);


            using (var stream =
                   new FileStream(
                       filePath,
                       FileMode.Create))
            {
                await model.ProfileImageFile.CopyToAsync(stream);
            }


            user.ProfileImage = fileName;
        }


        // Save changes

        var result = await _userManager.UpdateAsync(user);


        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(
                    string.Empty,
                    error.Description);
            }

            model.FullName = user.FullName;
            model.Email = user.Email;
            model.UserName = user.UserName;
            model.ProfileImage = user.ProfileImage;

            return View(model);
        }


        TempData["SuccessMessage"] =
            "Profile updated successfully.";


        return RedirectToAction(nameof(Index));
    }

    // GET: /Profile/ChangePassword
    [HttpGet]
    public IActionResult ChangePassword()
    {
        return View();
    }

    // POST: /Profile/ChangePassword
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }


        var user = await _userManager.GetUserAsync(User);

        if (user == null)
        {
            return Challenge();
        }


        var result = await _userManager.ChangePasswordAsync(
            user,
            model.CurrentPassword,
            model.NewPassword);


        if (result.Succeeded)
        {
            TempData["SuccessMessage"] =
                "Your password has been changed successfully.";

            return RedirectToAction(nameof(Index));
        }


        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(
                string.Empty,
                error.Description);
        }


        return View(model);
    }
}