using UsersAPI.Application.DTOs;
using UsersAPI.Application.Commands;
using UsersAPI.Domain.Entities;
using UsersAPI.Infrastructure;
using System.Text;
using System.Security.Cryptography;
using Microsoft.Extensions.Configuration;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Linq;

namespace UsersAPI.Application
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _repo;
        private readonly IRabbitMQPublisher _publisher;
        private readonly IConfiguration _configuration;

        public UserService(IUserRepository repo, IRabbitMQPublisher publisher, IConfiguration configuration)
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

            // publish UserCreatedEvent
            var @event = new { UserId = user.Id, Email = user.Email };
            _publisher.Publish("user.created", System.Text.Json.JsonSerializer.Serialize(@event));

            return new UserDto { Id = user.Id, Email = user.Email };
        }

        public Task<string> Authenticate(string email, string password)
        {
            var user = _repo.FindByEmail(email);
            if (user == null) return Task.FromResult<string?>(null!);

            if (!VerifyPassword(password, user.PasswordHash)) return Task.FromResult<string?>(null!);

            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_configuration["Jwt:Key"] ?? "super_secret_key_123!");
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new System.Security.Claims.ClaimsIdentity(new[] {
                    new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Email, user.Email),
                }.Concat(user.Roles.Select(r => new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, r)))),
                Expires = DateTime.UtcNow.AddHours(2),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return Task.FromResult(tokenHandler.WriteToken(token));
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
