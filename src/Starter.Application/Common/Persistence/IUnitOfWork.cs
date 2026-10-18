using Starter.Domain.Common;

namespace Starter.Application.Common.Persistence;

public interface IUnitOfWork
{
    /// <summary>Persists every staged change atomically: either all of them are saved or none.</summary>
    /// <returns>A domain error when a known constraint is violated (e.g. a concurrent duplicate).</returns>
    Task<Result> SaveChangesAsync(CancellationToken cancellationToken);
}
