using Microsoft.AspNetCore.Mvc;
using UsersAPI.Application.Commands.CreateUser;

namespace UsersAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController(CreateUserHandler createUserHandler) : ControllerBase
    {
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] CreateUserCommand command)
        {
            var user = await createUserHandler.Handle(command);
            return CreatedAtAction(nameof(Register), new { id = user.Id }, user);
        }
    }
}
