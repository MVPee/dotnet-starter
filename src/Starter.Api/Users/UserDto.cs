namespace Starter.Api.Users;

public sealed record UserDto(Guid Id, string Email, string FirstName, string LastName, DateTimeOffset CreatedAt);
