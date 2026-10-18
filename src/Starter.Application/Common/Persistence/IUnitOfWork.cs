using Starter.Domain.Common;

namespace Starter.Application.Common.Persistence;

public interface IUnitOfWork
{
    Task<Result> SaveChangesAsync(CancellationToken cancellationToken);
}
