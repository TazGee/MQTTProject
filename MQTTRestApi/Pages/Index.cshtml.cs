using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MQTTRestApi.Pages;

public class Index : PageModel
{
    public IActionResult OnGet()
    {
        return Page(); 
    } 
    public IActionResult OnPost()
    {
        return Page(); 
    }
}