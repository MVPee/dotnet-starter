using System.Text.Json;
using System.Text.Json.Serialization;

namespace Starter.IntegrationTests.Contracts;

internal static class WireJson
{
    // Case-sensitive camelCase so a PascalCase response fails to bind instead of silently passing.
    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };
}
