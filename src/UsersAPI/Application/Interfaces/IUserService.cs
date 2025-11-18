using UsersAPI.Application.DTOs;
using UsersAPI.Application.Commands;

namespace UsersAPI.Application
{
    public interface IUserService
    {
        Task<UserDto> Create(CreateUserCommand command);
        Task<string> Authenticate(string email, string password);
    }
}
