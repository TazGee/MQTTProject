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
    
    [HttpPost("login")]
    public IActionResult Login(LoginDto dto, [FromServices] ITokenService tokens)
    {
        (UserInfoDto user, bool success) = authService.Login(dto.Username, dto.Password);
        
        if (success)
        {
            var token = tokens.CreateToken(userId: user.Id.ToString(), username: dto.Username, role: user.Role.ToString());
            return Ok(new { token });
        }
        else
        {
            return BadRequest("Neuspesan pokusaj prijave!");
        }
    }
    
    [HttpPost("register")]
    public IActionResult Register(RegisterDto dto, [FromServices] ITokenService tokens)
    {
        (UserInfoDto user, bool success) = authService.Register(dto.Username, dto.Password, dto.UserType);
        
        if (success)
        {
            var token = tokens.CreateToken(userId: user.Id.ToString(), username: dto.Username, role: user.Role.ToString());
            return Ok(new { token });
        }
        else
        {
            return BadRequest("Neuspesan pokusaj registracije!");
        }
    }
    
    [Authorize(Roles = "Admin")]
    [HttpGet("list")]
    public IActionResult ListUsers()
    {
        List<UserInfoDto> listaKorisnika = usersService.GetUsers();
        
        return Ok(listaKorisnika);
    }
}