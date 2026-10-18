namespace Starter.Application.Common.Persistence;

// Every repository exposes exactly this CRUD surface; changes are only staged until IUnitOfWork saves them.
public interface IRepository<TModel, in TFilter>
    where TModel : class
    where TFilter : PagedFilter
{
    Task<TModel?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<TModel>> GetAllAsync(TFilter filter, CancellationToken cancellationToken);

    Task<int> GetCountAsync(TFilter filter, CancellationToken cancellationToken);

    Task AddBatchAsync(IReadOnlyCollection<TModel> models, CancellationToken cancellationToken);

    Task UpdateBatchAsync(IReadOnlyCollection<TModel> models, CancellationToken cancellationToken);

    Task DeleteBatchAsync(IReadOnlyCollection<TModel> models, CancellationToken cancellationToken);
}
