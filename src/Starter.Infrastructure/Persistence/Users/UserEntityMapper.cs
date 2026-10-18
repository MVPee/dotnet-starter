using Starter.Domain.Users;

namespace Starter.Infrastructure.Persistence.Users;

internal static class UserEntityMapper
{
    public static UserEntity ToEntity(this User user) => new()
    {
        Id = user.Id,
        Email = user.Email,
        FirstName = user.FirstName,
        LastName = user.LastName,
        // Npgsql rejects non-zero offsets for timestamptz columns.
        CreatedAt = user.CreatedAt.ToUniversalTime(),
    };

    public static User ToModel(this UserEntity userEntity)
        => User.Restore(userEntity.Id, userEntity.Email, userEntity.FirstName, userEntity.LastName, userEntity.CreatedAt);
}
