namespace UsersAPI.Application.Commands
{
    public class CreateUserCommand
    {
        public string Email { get; set; } = null!;
        public string Password { get; set; } = null!;
    }
}
