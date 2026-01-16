using UsersAPI.Domain.Entities;

namespace UsersAPI.Infrastructure
{
    public interface IUserRepository
    {
        Task SaveNewAsync(User user);
        Task<User?> FindByEmailAsync(string email);
    }
}
