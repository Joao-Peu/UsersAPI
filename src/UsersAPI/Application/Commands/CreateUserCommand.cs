namespace UsersAPI.Application.Commands
{
    public class CreateUserCommand
    {
        public required string Email { get; set; }
        public required string Password { get; set; }
    }
}
