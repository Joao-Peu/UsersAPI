using UsersAPI.Domain.Entities;
using System.Collections.Concurrent;

namespace UsersAPI.Infrastructure
{
    public class InMemoryUserRepository : IUserRepository
    {
        private readonly ConcurrentDictionary<string, User> _users = new();

        public Task Add(User user)
        {
            _users[user.Email] = user;
            return Task.CompletedTask;
        }

        public User? FindByEmail(string email)
        {
            _users.TryGetValue(email, out var user);
            return user;
        }
    }
}
