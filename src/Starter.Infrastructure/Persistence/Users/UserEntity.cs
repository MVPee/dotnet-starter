namespace Starter.Infrastructure.Persistence.Users;

internal sealed class UserEntity : IPersistenceEntity
{
    public Guid Id { get; set; }

    public required string Email { get; set; }

    public required string FirstName { get; set; }

    public required string LastName { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
