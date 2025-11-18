using UsersAPI.Domain.Entities;

namespace UsersAPI.Infrastructure
{
    public interface IUserRepository
    {
        Task Add(User user);
        User? FindByEmail(string email);
    }
}
