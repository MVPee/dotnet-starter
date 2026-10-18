using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Starter.Infrastructure.Persistence;

internal sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    private const string ConnectionStringEnvironmentVariable = "ConnectionStrings__Database";
    private const string RunningInContainerEnvironmentVariable = "DOTNET_RUNNING_IN_CONTAINER";
    private const string LocalDevelopmentConnectionString =
        "Host=localhost;Port=5432;Database=starter;Username=postgres;Password=postgres";

    public AppDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(ResolveConnectionString())
            .Options;

        return new AppDbContext(options);
    }

    private static string ResolveConnectionString()
    {
        var connectionString = Environment.GetEnvironmentVariable(ConnectionStringEnvironmentVariable);
        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            return connectionString;
        }

        // Set by official .NET images; inside a container a localhost fallback would hide a missing secret.
        var isRunningInContainer = bool.TryParse(Environment.GetEnvironmentVariable(RunningInContainerEnvironmentVariable), out var runningInContainer)
            && runningInContainer;
        if (isRunningInContainer)
        {
            throw new InvalidOperationException($"{ConnectionStringEnvironmentVariable} is required to run migrations in a container.");
        }

        return LocalDevelopmentConnectionString;
    }
}
