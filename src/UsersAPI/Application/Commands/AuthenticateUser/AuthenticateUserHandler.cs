using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using UsersAPI.Application.Interface;
using UsersAPI.Infrastructure.Interfaces;

namespace UsersAPI.Application.Commands.AuthenticateUser;

public class AuthenticateUserHandler(
    IUserRepository userRepository,
    IPasswordService passwordService,
    IConfiguration configuration)
{
    public async Task<string> Handle(AuthenticateUserCommand command)
    {
        var user = await userRepository.FindByEmailAsync(command.Email);
        if (user == null)
        {
            return string.Empty;
        }

        if (!passwordService.IsPasswordMatch(command.Password, user.Password.HashValue))
        {
            return string.Empty;
        }

        var jwtKey = configuration["Jwt:Key"] ?? "super_secret_key_123!";

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Name ?? string.Empty),
            new Claim(ClaimTypes.Role, user.Role.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty)
        };

        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(jwtKey)
        );

        var creds = new SigningCredentials(
            key,
            SecurityAlgorithms.HmacSha256
        );

        var token = new JwtSecurityToken(
            claims: claims,
            expires: DateTime.UtcNow.AddHours(2),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

