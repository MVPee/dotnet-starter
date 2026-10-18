using Microsoft.EntityFrameworkCore;
using Starter.Application.Common.Persistence;

namespace Starter.Infrastructure.Persistence;

internal abstract class Repository<TModel, TEntity, TFilter>(AppDbContext dbContext) : IRepository<TModel, TFilter>
    where TModel : class
    where TEntity : class, IPersistenceEntity
    where TFilter : PagedFilter
{
    private DbSet<TEntity> Entities => dbContext.Set<TEntity>();

    public async Task<TModel?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var matchingEntity = await Entities
            .AsNoTracking()
            .FirstOrDefaultAsync(entity => entity.Id == id, cancellationToken);

        return matchingEntity is null ? null : ToModel(matchingEntity);
    }

    public async Task<IReadOnlyList<TModel>> GetAllAsync(TFilter filter, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(filter);

        // Ids are Guid v7 (time-ordered), which gives a stable, creation-ordered page sequence.
        var entities = await ApplyFilter(Entities.AsNoTracking(), filter)
            .OrderBy(entity => entity.Id)
            .Skip(filter.Skip)
            .Take(filter.PageSize)
            .ToListAsync(cancellationToken);

        return entities.ConvertAll(ToModel);
    }

    public Task<int> GetCountAsync(TFilter filter, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(filter);

        return ApplyFilter(Entities.AsNoTracking(), filter).CountAsync(cancellationToken);
    }

    public Task AddBatchAsync(IReadOnlyCollection<TModel> models, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(models);

        Entities.AddRange(models.Select(ToEntity));
        return Task.CompletedTask;
    }

    public Task UpdateBatchAsync(IReadOnlyCollection<TModel> models, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(models);

        Entities.UpdateRange(models.Select(ToEntity));
        return Task.CompletedTask;
    }

    public Task DeleteBatchAsync(IReadOnlyCollection<TModel> models, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(models);

        Entities.RemoveRange(models.Select(ToEntity));
        return Task.CompletedTask;
    }

    protected abstract IQueryable<TEntity> ApplyFilter(IQueryable<TEntity> query, TFilter filter);

    protected abstract TModel ToModel(TEntity entity);

    protected abstract TEntity ToEntity(TModel model);
}
