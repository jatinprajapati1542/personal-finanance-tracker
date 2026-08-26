using Microsoft.AspNetCore.Mvc;

namespace personal_finance_Tracker.Controllers
{
    public class AccountController : Controller
    {
        [HttpGet("/login")]
        public IActionResult Login(string? returnUrl = null)
        {
            var loginUrl = "/Identity/Account/Login";

            if (!string.IsNullOrEmpty(returnUrl))
            {
                loginUrl += "?ReturnUrl=" +
                    Uri.EscapeDataString(returnUrl);
            }

            return Redirect(loginUrl);
        }

        [HttpGet("/register")]
        public IActionResult Register()
        {
            return Redirect("/Identity/Account/Register");
        }
    }
}