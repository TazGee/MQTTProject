using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MQTTRestApi.Services;

namespace MQTTRestApi.Pages;

public class Logout : PageModel
{
    public IActionResult OnPost()
    {
        AuthCookieService.Clear(Response);
        return RedirectToPage("/Login");
    }
}