using Microsoft.Extensions.Time.Testing;
using Starter.Application.Users.CreateUser;
using Starter.Domain.Users;
using Starter.UnitTests.Fakes;

namespace Starter.UnitTests.Application.Users.CreateUser;

public sealed class CreateUserUseCaseTests
{
    private const string Email = "  Jane.Doe@Example.COM ";
    private const string NormalizedEmail = "jane.doe@example.com";
    private const string ConflictingEmail = " JANE.DOE@example.com  ";
    private const string FirstName = "Jane";
    private const string LastName = "Doe";
    private const string OtherFirstName = "Janet";
    private const string OtherLastName = "Smith";

    private readonly InMemoryUserRepository _userRepository = new();
    private readonly FakeUnitOfWork _unitOfWork;
    private readonly CreateUserUseCase _useCase;

    public CreateUserUseCaseTests()
    {
        _unitOfWork = new FakeUnitOfWork(_userRepository);
        _useCase = new CreateUserUseCase(_userRepository, _unitOfWork, new FakeTimeProvider(TestClock.FixedNow));
    }

    [Fact]
    public async Task ExecuteAsync_WithNewEmail_ReturnsUserWithNormalizedEmailAndClockTime()
    {
        var result = await _useCase.ExecuteAsync(new CreateUserCommand(Email, FirstName, LastName), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(NormalizedEmail, result.Value.Email);
        Assert.Equal(FirstName, result.Value.FirstName);
        Assert.Equal(LastName, result.Value.LastName);
        Assert.Equal(TestClock.FixedNow, result.Value.CreatedAt);
    }

    [Fact]
    public async Task ExecuteAsync_WithNewEmail_PersistsUser()
    {
        var result = await _useCase.ExecuteAsync(new CreateUserCommand(Email, FirstName, LastName), CancellationToken.None);

        var persistedUser = Assert.Single(_userRepository.Users);
        Assert.Same(result.Value, persistedUser);
        Assert.Equal(1, _unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task ExecuteAsync_WhenSaveHitsConcurrentDuplicate_ReturnsEmailAlreadyUsed()
    {
        _unitOfWork.NextSaveResult = UserErrors.EmailAlreadyUsed;

        var result = await _useCase.ExecuteAsync(new CreateUserCommand(Email, FirstName, LastName), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(UserErrors.EmailAlreadyUsed, result.Error);
        Assert.Empty(_userRepository.Users);
    }

    [Fact]
    public async Task ExecuteAsync_WithEmailDifferingOnlyInCaseAndWhitespace_ReturnsEmailAlreadyUsed()
    {
        await _useCase.ExecuteAsync(new CreateUserCommand(Email, FirstName, LastName), CancellationToken.None);

        var result = await _useCase.ExecuteAsync(
            new CreateUserCommand(ConflictingEmail, OtherFirstName, OtherLastName),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(UserErrors.EmailAlreadyUsed, result.Error);
    }

    [Fact]
    public async Task ExecuteAsync_WithEmailAlreadyUsed_DoesNotPersistUser()
    {
        await _useCase.ExecuteAsync(new CreateUserCommand(Email, FirstName, LastName), CancellationToken.None);

        await _useCase.ExecuteAsync(new CreateUserCommand(ConflictingEmail, OtherFirstName, OtherLastName), CancellationToken.None);

        var persistedUser = Assert.Single(_userRepository.Users);
        Assert.Equal(FirstName, persistedUser.FirstName);
        Assert.Equal(1, _unitOfWork.SaveChangesCallCount);
    }
}
