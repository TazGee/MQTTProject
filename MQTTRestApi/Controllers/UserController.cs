using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MQTTRestApi.Domain.DTO;
using MQTTRestApi.Domain.Services;

namespace MQTTRestApi.Controllers;

/// <summary>
/// Controller sa endpointima za korisnike
/// </summary>
[ApiController]
[Route("api/users")]
public class UserController : ControllerBase
{
    IAuthService authService;
    IUsersService usersService;
    
    public UserController(IAuthService authService, IUsersService usersService)
    {
        this.authService = authService;
        this.usersService = usersService;
    }
    
    private int UserId()
    {
        return int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    }
    
    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginDto dto, [FromServices] ITokenService tokens)
    {
        var user = await authService.Login(dto.Username, dto.Password);
        
        var token = tokens.CreateToken(userId: user.Id.ToString(), username: dto.Username, role: user.Role.ToString());
        return Ok(new { token });
    }
    
    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterDto dto, [FromServices] ITokenService tokens)
    {
        var user = await authService.Register(dto.Username, dto.Password, dto.UserType);
        
        var token = tokens.CreateToken(userId: user.Id.ToString(), username: dto.Username, role: user.Role.ToString());
        return Ok(new { token });
    }
    
    [Authorize, HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword(ChangePasswordDto dto)
    {
        await usersService.ChangePassword(UserId(), dto);
        
        return Ok("Sifra uspesno promenjena.");
    }

    [Authorize(Roles = "Admin")]
    [HttpGet("list")]
    public IActionResult ListUsers()
    {
        List<UserInfoDto> listaKorisnika = usersService.GetUsers();
        
        return Ok(listaKorisnika);
    }
}