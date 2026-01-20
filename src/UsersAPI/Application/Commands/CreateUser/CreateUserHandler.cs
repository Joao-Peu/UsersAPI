using MassTransit;
using Shared.Events;
using UsersAPI.Application.Abstractions;
using UsersAPI.Application.DTOs;
using UsersAPI.Application.Interface;
using UsersAPI.Domain.Entities;
using UsersAPI.Domain.Enums;
using UsersAPI.Domain.ValueObjects;
using UsersAPI.Infrastructure.Interfaces;

namespace UsersAPI.Application.Commands.CreateUser;

public class CreateUserHandler(
    IUserRepository userRepository,
    IPasswordService passwordService,
    IPublishEndpoint publisher)
{
    public async Task<Result<UserDto>> Handle(CreateUserCommand command)
    {
        var validationResult = Validate(command);
        if (validationResult.IsFailure)
        {
            return Result<UserDto>.Failure(validationResult.Error);
        }

        var hash = passwordService.EncryptPassword(command.Password);
        var password = new Password(hash);

        if (await userRepository.FindByEmailAsync(command.Email) != null)
        {
            return Result<UserDto>.Failure(new Error("duplicate_user", $"Usuário já cadastrado para o email {command.Email}."));
        }

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

    private static Result Validate(CreateUserCommand command)
    {
        if (!Password.IsPasswordValid(command.Password))
        {
            return new Error("invalid_password","Senha deve ter no mínimo 8 caracteres, incluindo letras, números e caracteres especiais.");
        }

        if (string.IsNullOrWhiteSpace(command.Name))
        {
            return new Error("invalid_name","O nome deve ser informado.");
        }

        if (string.IsNullOrWhiteSpace(command.Email))
        {
            return new Error("invalid_email","O email deve ser informado.");
        }

        return Result.Success();
    }
}
