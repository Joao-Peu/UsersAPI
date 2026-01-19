using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UsersAPI.Application.Commands.CreateUser;

namespace UsersAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController(CreateUserHandler createUserHandler) : ControllerBase
    {
        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<IActionResult> Register([FromBody] CreateUserCommand command)
        {
            var userResult = await createUserHandler.Handle(command);
            if (userResult.IsFailure)
            {
                return BadRequest(userResult.Error);
            }

            return CreatedAtAction(nameof(Register), new { id = userResult.Value.Id }, userResult);
        }
    }
}
