using Microsoft.AspNetCore.Mvc;
using Starter.Domain.Common;

namespace Starter.Api.Errors;

internal static class ErrorProblemDetailsExtensions
{
    public const string ErrorCodeExtensionKey = "code";
    private const string UnmappedErrorTypeMessage = "Unmapped error type.";

    public static ObjectResult ToProblem(this ControllerBase controller, Error error)
    {
        var extensions = new Dictionary<string, object?> { [ErrorCodeExtensionKey] = error.Code };
        return controller.Problem(detail: error.Description, statusCode: ToStatusCode(error.Type), extensions: extensions);
    }

    private static int ToStatusCode(ErrorType errorType) => errorType switch
    {
        ErrorType.Validation => StatusCodes.Status400BadRequest,
        ErrorType.NotFound => StatusCodes.Status404NotFound,
        ErrorType.Conflict => StatusCodes.Status409Conflict,
        _ => throw new ArgumentOutOfRangeException(nameof(errorType), errorType, UnmappedErrorTypeMessage),
    };
}
