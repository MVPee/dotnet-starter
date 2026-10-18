using Starter.Domain.Common;

namespace Starter.UnitTests.Domain.Common;

public sealed class ResultTests
{
    private const string SuccessValue = "value";
    private const string ErrorCode = "test.failure";
    private const string ErrorDescription = "Something went wrong.";

    private static readonly Error TestError = Error.Validation(ErrorCode, ErrorDescription);

    [Fact]
    public void Success_AccessingValue_ReturnsValue()
    {
        var result = Result<string>.Success(SuccessValue);

        Assert.True(result.IsSuccess);
        Assert.Equal(SuccessValue, result.Value);
    }

    [Fact]
    public void Failure_AccessingError_ReturnsError()
    {
        var result = Result<string>.Failure(TestError);

        Assert.True(result.IsFailure);
        Assert.Equal(TestError, result.Error);
    }

    [Fact]
    public void Failure_AccessingValue_ThrowsInvalidOperationException()
    {
        var result = Result<string>.Failure(TestError);

        Assert.Throws<InvalidOperationException>(() => result.Value);
    }

    [Fact]
    public void Success_AccessingError_ThrowsInvalidOperationException()
    {
        var result = Result<string>.Success(SuccessValue);

        Assert.Throws<InvalidOperationException>(() => result.Error);
    }
}
