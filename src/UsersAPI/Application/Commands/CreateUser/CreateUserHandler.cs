using MassTransit;
using UsersAPI.Application.DTOs;
using UsersAPI.Application.Events;
using UsersAPI.Application.Interface;
using UsersAPI.Domain.Entities;
using UsersAPI.Domain.Enums;
using UsersAPI.Domain.ValueObjects;
using UsersAPI.Infrastructure;

namespace UsersAPI.Application.Commands.CreateUser;

public class CreateUserHandler(
    IUserRepository userRepository,
    IPasswordService passwordService,
    IPublishEndpoint publisher)
{
    public async Task<UserDto> Handle(CreateUserCommand command)
    {
        Validate(command);

        var hash = passwordService.EncryptPassword(command.Password);
        var password = new Password(hash);

        var user = new User(
            command.Name,
            command.Email,
            password,
            UserRole.User
        );

        await userRepository.SaveNewAsync(user);

        var @event = new UserCreatedEvent
        {
            UserId = user.Id,
            Email = user.Email
        };

        await publisher.Publish(@event);

        return new UserDto
        {
            Id = user.Id,
            Email = user.Email
        };
    }

    private static void Validate(CreateUserCommand command)
    {
        if (!Password.IsPasswordValid(command.Password))
        {
            throw new Exception("Senha deve ter no mínimo 8 caracteres, incluindo letras, números e caracteres especiais.");
        }

        if (string.IsNullOrWhiteSpace(command.Name))
        {
            throw new Exception("O nome deve ser informado.");
        }

        if (string.IsNullOrWhiteSpace(command.Email))
        {
            throw new Exception("O email deve ser informado.");
        }
    }
}
