using Starter.Domain.Common;

namespace Starter.Domain.Users;

public sealed class User
{
    private User(Guid id, string email, string firstName, string lastName, DateTimeOffset createdAt)
    {
        Id = id;
        Email = email;
        FirstName = firstName;
        LastName = lastName;
        CreatedAt = createdAt;
    }

    public Guid Id { get; }

    public string Email { get; }

    public string FirstName { get; }

    public string LastName { get; }

    public DateTimeOffset CreatedAt { get; }

    public static Result<User> Create(string email, string firstName, string lastName, DateTimeOffset createdAt)
    {
        if (!EmailAddressRule.IsValid(email))
        {
            return UserErrors.InvalidEmail;
        }

        if (!PersonNameRule.IsValid(firstName))
        {
            return UserErrors.InvalidFirstName;
        }

        if (!PersonNameRule.IsValid(lastName))
        {
            return UserErrors.InvalidLastName;
        }

        return new User(
            Guid.CreateVersion7(createdAt),
            EmailAddressRule.Normalize(email),
            PersonNameRule.Normalize(firstName),
            PersonNameRule.Normalize(lastName),
            createdAt);
    }

    public static User Restore(Guid id, string email, string firstName, string lastName, DateTimeOffset createdAt)
        => new(id, email, firstName, lastName, createdAt);
}
