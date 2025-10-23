using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Zirku.Server.Pages.Account;

public class LogoutModel : PageModel
{
    public void OnGet()
    {
        // Display logout confirmation page
    }

    public async Task<IActionResult> OnPostAsync()
    {
        // Sign out the user (remove cookie)
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        return Redirect("/Account/Login");
    }
}

