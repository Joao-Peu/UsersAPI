using UsersAPI.Domain.Enums;

namespace UsersAPI.Application.Commands.CreateUser;

public record CreateUserCommand
{
    public required string Name { get; set; }
    public required string Email { get; set; }
    public required string Password { get; set; }
}
