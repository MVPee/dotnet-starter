using System.Net.Http.Json;
using Starter.IntegrationTests.Contracts;

namespace Starter.IntegrationTests.Users;

internal static class UsersApi
{
    public const string BasePath = "/api/users";

    public static Task<HttpResponseMessage> CreateAsync(this HttpClient client, CreateUserRequest request)
        => client.PostAsJsonAsync(BasePath, request, WireJson.Options);

    public static Task<HttpResponseMessage> ListAsync(this HttpClient client, string queryString)
        => client.GetAsync(new Uri($"{BasePath}?{queryString}", UriKind.Relative));

    public static Task<HttpResponseMessage> GetByIdAsync(this HttpClient client, Guid userId)
        => client.GetAsync(new Uri($"{BasePath}/{userId}", UriKind.Relative));

    public static async Task<T> ReadAsAsync<T>(this HttpResponseMessage response)
    {
        var body = await response.Content.ReadFromJsonAsync<T>(WireJson.Options);
        Assert.NotNull(body);
        return body;
    }
}
