using Starter.Application.Users;
using Starter.Application.Users.GetUsers;
using Starter.Domain.Users;
using Starter.UnitTests.Fakes;

namespace Starter.UnitTests.Application.Users.GetUsers;

public sealed class GetUsersUseCaseTests
{
    private const string FirstName = "Jane";
    private const string LastName = "Doe";
    private const string TargetEmail = "target@example.com";
    private const string TargetEmailWithNoise = "  TARGET@Example.com ";
    private const int SeededUserCount = 5;
    private const int PageSize = 2;
    private const int LastPage = 3;

    private readonly InMemoryUserRepository _userRepository = new();
    private readonly GetUsersUseCase _useCase;

    public GetUsersUseCaseTests()
    {
        _useCase = new GetUsersUseCase(_userRepository);
    }

    [Fact]
    public async Task ExecuteAsync_WithUnnormalizedEmailFilter_ReturnsMatchingUserOnly()
    {
        SeedUsers();
        _userRepository.Seed(User.Create(TargetEmail, FirstName, LastName, TestClock.FixedNow).Value);

        var pagedUsers = await _useCase.ExecuteAsync(new UserFilter { Email = TargetEmailWithNoise }, CancellationToken.None);

        var matchingUser = Assert.Single(pagedUsers.Items);
        Assert.Equal(TargetEmail, matchingUser.Email);
        Assert.Equal(1, pagedUsers.TotalCount);
    }

    [Fact]
    public async Task ExecuteAsync_WithLastPage_ReturnsRemainingUsersAndTotalCount()
    {
        SeedUsers();

        var pagedUsers = await _useCase.ExecuteAsync(new UserFilter { Page = LastPage, PageSize = PageSize }, CancellationToken.None);

        Assert.Single(pagedUsers.Items);
        Assert.Equal(SeededUserCount, pagedUsers.TotalCount);
        Assert.Equal(LastPage, pagedUsers.Page);
        Assert.Equal(PageSize, pagedUsers.PageSize);
    }

    private void SeedUsers()
        => _userRepository.Seed([.. Enumerable.Range(0, SeededUserCount)
            .Select(userIndex => User.Create($"user-{userIndex}@example.com", FirstName, LastName, TestClock.FixedNow).Value)]);
}
