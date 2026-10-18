using Starter.Application.Users;
using Starter.Domain.Users;

namespace Starter.UnitTests.Fakes;

internal sealed class InMemoryUserRepository : IUserRepository, IStagedStore
{
    private readonly List<User> _users = [];
    private readonly List<Action<List<User>>> _stagedChanges = [];

    public IReadOnlyList<User> Users => _users;

    public void Seed(params User[] users) => _users.AddRange(users);

    public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => Task.FromResult(_users.Find(user => user.Id == id));

    public Task<IReadOnlyList<User>> GetAllAsync(UserFilter filter, CancellationToken cancellationToken)
    {
        IReadOnlyList<User> page = [.. ApplyFilter(filter).OrderBy(user => user.Id).Skip(filter.Skip).Take(filter.PageSize)];
        return Task.FromResult(page);
    }

    public Task<int> GetCountAsync(UserFilter filter, CancellationToken cancellationToken)
        => Task.FromResult(ApplyFilter(filter).Count());

    public Task AddBatchAsync(IReadOnlyCollection<User> models, CancellationToken cancellationToken)
    {
        _stagedChanges.Add(users => users.AddRange(models));
        return Task.CompletedTask;
    }

    public Task UpdateBatchAsync(IReadOnlyCollection<User> models, CancellationToken cancellationToken)
    {
        _stagedChanges.Add(users =>
        {
            foreach (var updatedUser in models)
            {
                users[users.FindIndex(user => user.Id == updatedUser.Id)] = updatedUser;
            }
        });
        return Task.CompletedTask;
    }

    public Task DeleteBatchAsync(IReadOnlyCollection<User> models, CancellationToken cancellationToken)
    {
        var deletedUserIds = models.Select(user => user.Id).ToHashSet();
        _stagedChanges.Add(users => users.RemoveAll(user => deletedUserIds.Contains(user.Id)));
        return Task.CompletedTask;
    }

    public void CommitStagedChanges()
    {
        foreach (var stagedChange in _stagedChanges)
        {
            stagedChange(_users);
        }

        _stagedChanges.Clear();
    }

    public void DiscardStagedChanges() => _stagedChanges.Clear();

    private IEnumerable<User> ApplyFilter(UserFilter filter)
        => filter.Email is null ? _users : _users.Where(user => user.Email == filter.Email);
}
