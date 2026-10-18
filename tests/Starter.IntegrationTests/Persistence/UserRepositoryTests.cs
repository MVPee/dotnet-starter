using Microsoft.Extensions.DependencyInjection;
using Starter.Application.Common.Persistence;
using Starter.Application.Users;
using Starter.Domain.Common;
using Starter.Domain.Users;
using Starter.IntegrationTests.Fixtures;

namespace Starter.IntegrationTests.Persistence;

[Collection(ApiCollectionDefinition.Name)]
public sealed class UserRepositoryTests(ApiFactory factory)
{
    private const string FirstName = "Ada";
    private const string LastName = "Lovelace";
    private const string UpdatedFirstName = "Augusta";

    private static readonly DateTimeOffset CreatedAt = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task AddBatch_WithoutSaveChanges_PersistsNothing()
    {
        var user = NewUser();
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            await Repository(scope).AddBatchAsync([user], CancellationToken.None);
        }

        Assert.Null(await FindAsync(user.Id));
    }

    [Fact]
    public async Task AddBatch_ThenSaveChanges_PersistsEveryUser()
    {
        User[] users = [NewUser(), NewUser()];

        var saveResult = await StageAndSaveAsync((repository, cancellationToken) => repository.AddBatchAsync(users, cancellationToken));

        Assert.True(saveResult.IsSuccess);
        foreach (var user in users)
        {
            Assert.NotNull(await FindAsync(user.Id));
        }
    }

    [Fact]
    public async Task SaveChanges_WhenOneInsertViolatesUniqueEmail_PersistsNoneAndReturnsDomainError()
    {
        var email = TestEmails.Unique();
        var validUser = NewUser();
        var firstDuplicate = User.Create(email, FirstName, LastName, CreatedAt).Value;
        var secondDuplicate = User.Create(email, FirstName, LastName, CreatedAt).Value;

        var saveResult = await StageAndSaveAsync((repository, cancellationToken) =>
            repository.AddBatchAsync([validUser, firstDuplicate, secondDuplicate], cancellationToken));

        Assert.True(saveResult.IsFailure);
        Assert.Equal(UserErrors.EmailAlreadyUsed, saveResult.Error);
        Assert.Null(await FindAsync(validUser.Id));
        Assert.Null(await FindAsync(firstDuplicate.Id));
    }

    [Fact]
    public async Task SaveChanges_AfterMappedViolation_CanSaveAgainInSameScope()
    {
        var email = TestEmails.Unique();
        var validUser = NewUser();
        await using var scope = factory.Services.CreateAsyncScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        await Repository(scope).AddBatchAsync(
            [User.Create(email, FirstName, LastName, CreatedAt).Value, User.Create(email, FirstName, LastName, CreatedAt).Value],
            CancellationToken.None);
        var failedSave = await unitOfWork.SaveChangesAsync(CancellationToken.None);

        await Repository(scope).AddBatchAsync([validUser], CancellationToken.None);
        var retriedSave = await unitOfWork.SaveChangesAsync(CancellationToken.None);

        Assert.True(failedSave.IsFailure);
        Assert.True(retriedSave.IsSuccess);
        Assert.NotNull(await FindAsync(validUser.Id));
    }

    [Fact]
    public async Task UpdateBatch_OfUnknownUser_ReturnsConcurrencyConflict()
    {
        var saveResult = await StageAndSaveAsync((repository, cancellationToken) => repository.UpdateBatchAsync([NewUser()], cancellationToken));

        Assert.Equal(PersistenceErrors.ConcurrencyConflict, saveResult.Error);
    }

    [Fact]
    public async Task DeleteBatch_OfUnknownUser_ReturnsConcurrencyConflict()
    {
        var saveResult = await StageAndSaveAsync((repository, cancellationToken) => repository.DeleteBatchAsync([NewUser()], cancellationToken));

        Assert.Equal(PersistenceErrors.ConcurrencyConflict, saveResult.Error);
    }

    [Fact]
    public async Task UpdateBatch_WithChangedCreatedAt_KeepsOriginalCreatedAt()
    {
        var user = NewUser();
        await StageAndSaveAsync((repository, cancellationToken) => repository.AddBatchAsync([user], cancellationToken));
        var tamperedUser = User.Restore(user.Id, user.Email, user.FirstName, user.LastName, user.CreatedAt.AddYears(1));

        await StageAndSaveAsync((repository, cancellationToken) => repository.UpdateBatchAsync([tamperedUser], cancellationToken));

        Assert.Equal(user.CreatedAt, (await FindAsync(user.Id))?.CreatedAt);
    }

    [Fact]
    public async Task UpdateBatch_ThenSaveChanges_PersistsChanges()
    {
        var user = NewUser();
        await StageAndSaveAsync((repository, cancellationToken) => repository.AddBatchAsync([user], cancellationToken));
        var renamedUser = User.Restore(user.Id, user.Email, UpdatedFirstName, user.LastName, user.CreatedAt);

        var saveResult = await StageAndSaveAsync((repository, cancellationToken) => repository.UpdateBatchAsync([renamedUser], cancellationToken));

        Assert.True(saveResult.IsSuccess);
        Assert.Equal(UpdatedFirstName, (await FindAsync(user.Id))?.FirstName);
    }

    [Fact]
    public async Task DeleteBatch_ThenSaveChanges_RemovesUsers()
    {
        var user = NewUser();
        await StageAndSaveAsync((repository, cancellationToken) => repository.AddBatchAsync([user], cancellationToken));

        var saveResult = await StageAndSaveAsync((repository, cancellationToken) => repository.DeleteBatchAsync([user], cancellationToken));

        Assert.True(saveResult.IsSuccess);
        Assert.Null(await FindAsync(user.Id));
    }

    [Fact]
    public async Task GetAllAndGetCount_WithEmailFilter_ReturnOnlyMatchingUser()
    {
        var user = NewUser();
        await StageAndSaveAsync((repository, cancellationToken) => repository.AddBatchAsync([user, NewUser()], cancellationToken));
        var emailFilter = new UserFilter { Email = user.Email };

        await using var scope = factory.Services.CreateAsyncScope();
        var matchingUsers = await Repository(scope).GetAllAsync(emailFilter, CancellationToken.None);
        var matchingCount = await Repository(scope).GetCountAsync(emailFilter, CancellationToken.None);

        Assert.Equal(user.Id, Assert.Single(matchingUsers).Id);
        Assert.Equal(1, matchingCount);
    }

    private static User NewUser() => User.Create(TestEmails.Unique(), FirstName, LastName, CreatedAt).Value;

    private static IUserRepository Repository(AsyncServiceScope scope) => scope.ServiceProvider.GetRequiredService<IUserRepository>();

    private async Task<Result> StageAndSaveAsync(Func<IUserRepository, CancellationToken, Task> stageChanges)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        await stageChanges(Repository(scope), CancellationToken.None);
        return await scope.ServiceProvider.GetRequiredService<IUnitOfWork>().SaveChangesAsync(CancellationToken.None);
    }

    private async Task<User?> FindAsync(Guid userId)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        return await Repository(scope).GetByIdAsync(userId, CancellationToken.None);
    }
}
