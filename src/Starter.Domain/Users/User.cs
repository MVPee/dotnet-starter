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

    public static User Create(string email, string firstName, string lastName, DateTimeOffset createdAt)
        => new(Guid.CreateVersion7(createdAt), NormalizeEmail(email), firstName.Trim(), lastName.Trim(), createdAt);

    // Rehydration from persistence: values were validated when the user was created.
    public static User Restore(Guid id, string email, string firstName, string lastName, DateTimeOffset createdAt)
        => new(id, email, firstName, lastName, createdAt);

    public static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();
}
