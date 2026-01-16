using UsersAPI.Domain.Enums;

namespace UsersAPI.Application.Commands.AuthenticateUser;

public record AuthenticateUserCommand
{
    public required string Email { get; set; }
    public required string Password { get; set; }
}
