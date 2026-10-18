using Starter.Domain.Common;
using Starter.Domain.Users;

namespace Starter.Application.Users.GetUserById;

public sealed class GetUserByIdUseCase(IUserRepository userRepository)
{
    public async Task<Result<User>> ExecuteAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(userId, cancellationToken);
        return user is null ? UserErrors.NotFound(userId) : user;
    }
}
