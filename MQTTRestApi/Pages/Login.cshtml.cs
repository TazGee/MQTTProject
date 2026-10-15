using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MQTTRestApi.Domain.DTO;
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

    [BindProperty] public LoginInput LoginForm { get; set; } = new();
    [BindProperty] public RegisterInput RegisterForm { get; set; } = new();
    public string? LoginError { get; set; }
    public string? RegisterError { get; set; }
    public class LoginInput
    {
        public string Username { get; set; } = "";
        public string Password { get; set; } = "";
    }

    public class RegisterInput
    {
        public string Username { get; set; } = "";
        public string Password { get; set; } = "";
    }

    public async Task<IActionResult> OnPostLoginAsync(string? returnUrl)
    {
        try
        {
            var user = await authService.Login(LoginForm.Username, LoginForm.Password);
            return SignInAndRedirect(user, returnUrl);
        }
        catch (BadRequestException e)
        {
            LoginError = e.Message;
            return Page();
        }
    }

    public async Task<IActionResult> OnPostRegisterAsync(string? returnUrl)
    {
        try
        {
            var user = await authService.Register(RegisterForm.Username, RegisterForm.Password, UserTypes.User);
            return SignInAndRedirect(user, returnUrl);
        }
        catch (BadRequestException e)
        {
            RegisterError = e.Message;
            return Page();
        }
    }
    
    private IActionResult SignInAndRedirect(UserInfoDto user, string? returnUrl)
    {
        var token = tokenService.CreateToken(user.Id.ToString(), user.Username, user.Role.ToString());
        AuthCookieService.Set(Response, token, config);
        return LocalRedirect(Url.IsLocalUrl(returnUrl) ? returnUrl! : "/");
    }
}