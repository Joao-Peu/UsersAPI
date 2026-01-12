using MassTransit;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using UsersAPI.Application.Commands;
using UsersAPI.Application.DTOs;
using UsersAPI.Application.Events;
using UsersAPI.Domain.Entities;
using UsersAPI.Infrastructure;

namespace UsersAPI.Application
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _repo;
        private readonly IPublishEndpoint _publisher;
        private readonly IConfiguration _configuration;

        public UserService(IUserRepository repo, IConfiguration configuration, IPublishEndpoint publisher)
        {
            _repo = repo;
            _publisher = publisher;
            _configuration = configuration;
        }

        public async Task<UserDto> Create(CreateUserCommand command)
        {
            var hash = HashPassword(command.Password);
            var user = new User(command.Email, hash);
            await _repo.Add(user);

            var @event = new UserCreatedEvent { UserId = user.Id, Email = user.Email };
            await _publisher.Publish(@event);

            return new UserDto { Id = user.Id, Email = user.Email };
        }

        public string Authenticate(string email, string password)
        {
            var user = _repo.FindByEmail(email);
            if (user == null)
            {
                return string.Empty;
            }

            if (!VerifyPassword(password, user.PasswordHash)) 
            {
                return string.Empty; 
            }
            
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_configuration["Jwt:Key"] ?? "super_secret_key_123!");
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[] {
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Email, user.Email),
                }.Concat(user.Roles.Select(r => new Claim(ClaimTypes.Role, r)))),
                Expires = DateTime.UtcNow.AddHours(2),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        private string HashPassword(string password)
        {
            using var sha = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(password);
            var hash = sha.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }

        private bool VerifyPassword(string password, string hash)
        {
            return HashPassword(password) == hash;
        }
    }
}
