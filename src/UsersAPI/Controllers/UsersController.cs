using Microsoft.AspNetCore.Mvc;
using UsersAPI.Application;
using UsersAPI.Application.Commands;
using UsersAPI.Application.DTOs;

namespace UsersAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _service;

        public UsersController(IUserService service)
        {
            _service = service;
        }

        [HttpPost("register")] 
        public async Task<IActionResult> Register([FromBody] CreateUserCommand cmd)
        {
            var user = await _service.Create(cmd);
            return CreatedAtAction(nameof(Register), new { id = user.Id }, user);
        }

        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginRequest request)
        {
            var token = _service.Authenticate(request.Email, request.Password);
            if (string.IsNullOrWhiteSpace(token))
            {
                return Unauthorized();
            }

            return Ok(new { token });
        }
    }

    public class LoginRequest { public string Email { get; set; } = null!; public string Password { get; set; } = null!; }
}
