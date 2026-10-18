using Starter.Application.Users;
using Starter.Domain.Users;

namespace Starter.Infrastructure.Persistence.Users;

internal sealed class UserRepository(AppDbContext dbContext)
    : Repository<User, UserEntity, UserFilter>(dbContext), IUserRepository
{
    protected override IQueryable<UserEntity> ApplyFilter(IQueryable<UserEntity> query, UserFilter filter)
    {
        if (filter.Email is not null)
        {
            query = query.Where(userEntity => userEntity.Email == filter.Email);
        }

        return query;
    }

    protected override User ToModel(UserEntity entity) => entity.ToModel();

    protected override UserEntity ToEntity(User model) => model.ToEntity();
}
