using Microsoft.AspNetCore.Mvc;
using UsersAPI.Application.Commands.AuthenticateUser;

namespace UsersAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(AuthenticateUserHandler _authenticateUserHandler) : ControllerBase
{
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] AuthenticateUserCommand command)
    {
        var token = await _authenticateUserHandler.Handle(command);
        if (string.IsNullOrWhiteSpace(token))
        {
            return Unauthorized();
        }

        return Ok(new { token });
    }
}
