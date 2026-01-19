using UsersAPI.Domain.Enums;
using UsersAPI.Domain.ValueObjects;

namespace UsersAPI.Domain.Entities
{
    public class User
    {
        public Guid Id { get; private set; }
        public string Name { get; private set; }
        public string Email { get; private set; }
        public Password Password { get; private set; }
        public UserRole Role { get; private set; }
        public bool IsActive { get; private set; } = true;

        private User()
        {

        }

        public User(string name, string email, Password password, UserRole role = UserRole.User)
        {
            Id = Guid.NewGuid();
            IsActive = true;
            Name = name;
            Email = email;
            Password = password;
            Role = role;
        }
    }
}
