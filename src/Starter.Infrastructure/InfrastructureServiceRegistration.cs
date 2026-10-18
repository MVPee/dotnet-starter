using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Starter.Application.Common.Persistence;
using Starter.Application.Users;
using Starter.Infrastructure.Persistence;
using Starter.Infrastructure.Persistence.Users;

namespace Starter.Infrastructure;

public static class InfrastructureServiceRegistration
{
    public const string DatabaseConnectionStringName = "Database";

    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(DatabaseConnectionStringName);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                $"Connection string '{DatabaseConnectionStringName}' is not configured. " +
                $"Set 'ConnectionStrings:{DatabaseConnectionStringName}' in configuration.");
        }

        services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IUserRepository, UserRepository>();

        services.AddHealthChecks().AddDbContextCheck<AppDbContext>();

        return services;
    }

    public static async Task ApplyDatabaseMigrationsAsync(this IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await dbContext.Database.MigrateAsync(cancellationToken);
    }
}
