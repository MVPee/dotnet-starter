namespace Starter.Application.Users.CreateUser;

public sealed record CreateUserCommand(string Email, string FirstName, string LastName);
