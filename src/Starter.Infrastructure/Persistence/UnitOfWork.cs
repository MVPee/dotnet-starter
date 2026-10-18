using Microsoft.EntityFrameworkCore;
using Npgsql;
using Starter.Application.Common.Persistence;
using Starter.Domain.Common;

namespace Starter.Infrastructure.Persistence;

internal sealed class UnitOfWork(AppDbContext dbContext) : IUnitOfWork
{
    public async Task<Result> SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
        catch (DbUpdateConcurrencyException)
        {
            dbContext.ChangeTracker.Clear();
            return PersistenceErrors.ConcurrencyConflict;
        }
        catch (DbUpdateException exception)
        {
            // A failed save leaves staged entities tracked; clear them so the scope can be reused.
            dbContext.ChangeTracker.Clear();
            if (exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } postgresException
                && UniqueConstraintErrors.TryGetError(postgresException.ConstraintName, out var error))
            {
                return error;
            }

            throw;
        }
    }
}
