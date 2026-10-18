using Starter.Application.Common.Persistence;
using Starter.Domain.Common;

namespace Starter.UnitTests.Fakes;

internal sealed class FakeUnitOfWork(params IStagedStore[] stagedStores) : IUnitOfWork
{
    public int SaveChangesCallCount { get; private set; }

    public Result NextSaveResult { get; set; } = Result.Success();

    public Task<Result> SaveChangesAsync(CancellationToken cancellationToken)
    {
        SaveChangesCallCount++;
        foreach (var stagedStore in stagedStores)
        {
            if (NextSaveResult.IsSuccess)
            {
                stagedStore.CommitStagedChanges();
            }
            else
            {
                stagedStore.DiscardStagedChanges();
            }
        }

        return Task.FromResult(NextSaveResult);
    }
}
