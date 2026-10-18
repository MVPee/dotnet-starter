using Starter.Application.Users.GetUserById;
using Starter.Domain.Users;
using Starter.UnitTests.Fakes;

namespace Starter.UnitTests.Application.Users.GetUserById;

public sealed class GetUserByIdUseCaseTests
{
    private const string Email = "jane.doe@example.com";
    private const string FirstName = "Jane";
    private const string LastName = "Doe";

    private readonly InMemoryUserRepository _userRepository = new();
    private readonly GetUserByIdUseCase _useCase;

    public GetUserByIdUseCaseTests()
    {
        _useCase = new GetUserByIdUseCase(_userRepository);
    }

    [Fact]
    public async Task ExecuteAsync_WithExistingId_ReturnsUser()
    {
        var existingUser = User.Create(Email, FirstName, LastName, TestClock.FixedNow).Value;
        _userRepository.Seed(existingUser);

        var result = await _useCase.ExecuteAsync(existingUser.Id, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Same(existingUser, result.Value);
    }

    [Fact]
    public async Task ExecuteAsync_WithUnknownId_ReturnsNotFoundError()
    {
        var unknownUserId = Guid.CreateVersion7();

        var result = await _useCase.ExecuteAsync(unknownUserId, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(UserErrors.NotFound(unknownUserId), result.Error);
    }
}
