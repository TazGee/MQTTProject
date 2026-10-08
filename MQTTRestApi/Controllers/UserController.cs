using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MQTTRestApi.Data;
using MQTTRestApi.Domain.DTO;
using MQTTRestApi.Domain.Models;
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
        (User user, bool success) = authService.Login(dto.Username, dto.Password);
        
        Console.WriteLine(user.Username + user.Password + " " + success);
        
        if (success)
        {
            Console.WriteLine("Uspesan login");
            var token = tokens.CreateToken(userId: user.Id.ToString(), username: dto.Username, role: user.Role.ToString());
            return Ok(new { token });
        }
        else
        {
            return BadRequest();
        }
    }
    
    [HttpPost("register")]
    public IActionResult Login(RegisterDto dto, [FromServices] ITokenService tokens)
    {
        (User user, bool success) = authService.Register(dto.Username, dto.Password, dto.UserType);
        
        Console.WriteLine(user.Username + user.Password + " " + success);
        
        if (success)
        {
            Console.WriteLine("Uspesna registracija");
            var token = tokens.CreateToken(userId: user.Id.ToString(), username: dto.Username, role: user.Role.ToString());
            return Ok(new { token });
        }
        else
        {
            return BadRequest();
        }
    }
    
    [Authorize(Roles = "Admin")]
    [HttpPost("list")]
    public IActionResult ListUsers()
    {
        List<User> listaKorisnika = usersService.GetUsers();
        
        return Ok(listaKorisnika);
    }
}