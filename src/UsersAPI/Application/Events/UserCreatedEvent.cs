namespace UsersAPI.Application.Events;

public class UserCreatedEvent
{
    public required Guid UserId { get; set; }
    public required string Email { get; set; }
}
