using System;

namespace UsersAPI.Domain.Entities
{
    public class User
    {
        public Guid Id { get; private set; }
        public string Email { get; private set; }
        public string PasswordHash { get; private set; }
        public string[] Roles { get; private set; }

        public User(string email, string passwordHash, string[]? roles = null)
        {
            Id = Guid.NewGuid();
            Email = email;
            PasswordHash = passwordHash;
            Roles = roles ?? new string[] { "User" };
        }

        public void SetPasswordHash(string hash) => PasswordHash = hash;
    }
}
