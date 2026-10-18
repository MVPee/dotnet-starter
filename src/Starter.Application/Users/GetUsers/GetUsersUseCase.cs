using Starter.Application.Common.Persistence;
using Starter.Domain.Users;

namespace Starter.Application.Users.GetUsers;

public sealed class GetUsersUseCase(IUserRepository userRepository)
{
    public async Task<PagedList<User>> ExecuteAsync(UserFilter filter, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(filter);

        var users = await userRepository.GetAllAsync(filter, cancellationToken);
        var totalCount = await userRepository.GetCountAsync(filter, cancellationToken);

        return new PagedList<User>(users, filter.Page, filter.PageSize, totalCount);
    }
}
