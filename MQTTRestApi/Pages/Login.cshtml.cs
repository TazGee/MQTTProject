using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MQTTRestApi.Domain.Enums;
using MQTTRestApi.Domain.Exceptions;
using MQTTRestApi.Domain.Services;
using MQTTRestApi.Services;

namespace MQTTRestApi.wwwroot.Pages;

public class Login : PageModel
{
    private readonly IAuthService authService;
    private readonly ITokenService tokenService;
    private readonly IConfiguration config;

    public Login(IAuthService authService, ITokenService tokenService, IConfiguration config)
    {
        this.authService = authService;
        this.tokenService = tokenService;
        this.config = config;
    }

    [BindProperty] public string Username { get; set; } = "";
    [BindProperty] public string Password { get; set; } = "";
    public string? Error { get; set; }

    public async Task<IActionResult> OnPostAsync(string? returnUrl)
    {
        try
        {
            var user = await authService.Login(Username, Password);
            var token = tokenService.CreateToken(user.Id.ToString(), user.Username, user.Role.ToString());

            AuthCookieService.Set(Response, token, config);
            return LocalRedirect(Url.IsLocalUrl(returnUrl) ? returnUrl! : "/");
        }
        catch (BadRequestException e)
        {
            Error = e.Message;
            return Page();
        }
    }
}