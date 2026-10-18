using Starter.Application.Common.Persistence;
using Starter.Domain.Common;
using Starter.Domain.Users;

namespace Starter.Application.Users.CreateUser;

public sealed class CreateUserUseCase(IUserRepository userRepository, IUnitOfWork unitOfWork, TimeProvider timeProvider)
{
    public async Task<Result<User>> ExecuteAsync(CreateUserCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var userResult = User.Create(command.Email, command.FirstName, command.LastName, timeProvider.GetUtcNow());
        if (userResult.IsFailure)
        {
            return userResult.Error;
        }

        var user = userResult.Value;
        var emailFilter = new UserFilter { Email = user.Email };
        if (await userRepository.GetCountAsync(emailFilter, cancellationToken) > 0)
        {
            return UserErrors.EmailAlreadyUsed;
        }

        await userRepository.AddBatchAsync([user], cancellationToken);

        var saveResult = await unitOfWork.SaveChangesAsync(cancellationToken);
        return saveResult.IsSuccess ? user : saveResult.Error;
    }
}
