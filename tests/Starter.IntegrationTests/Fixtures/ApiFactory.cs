using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Testcontainers.PostgreSql;

namespace Starter.IntegrationTests.Fixtures;

public sealed class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private const string PostgresImage = "postgres:17-alpine";
    private const string ConnectionStringKey = "ConnectionStrings:Database";
    private const string ApplyMigrationsOnStartupKey = "Database:ApplyMigrationsOnStartup";
    private const string EnabledSettingValue = "true";
    // Every test client shares one partition (TestServer has no remote IP), so limits must not interfere with functional tests.
    private const string EffectivelyUnlimitedPermits = "100000";

    private readonly PostgreSqlContainer _databaseContainer = new PostgreSqlBuilder(PostgresImage).Build();

    public Task InitializeAsync() => _databaseContainer.StartAsync();

    async Task IAsyncLifetime.DisposeAsync()
    {
        await DisposeAsync();
        await _databaseContainer.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // UseSetting feeds host configuration, which Program reads before Build() when registering Infrastructure.
        builder.UseSetting(ConnectionStringKey, _databaseContainer.GetConnectionString());
        builder.UseSetting(ApplyMigrationsOnStartupKey, EnabledSettingValue);
        builder.UseSetting(ApiSettingKeys.ReadPermitLimit, EffectivelyUnlimitedPermits);
        builder.UseSetting(ApiSettingKeys.WritePermitLimit, EffectivelyUnlimitedPermits);
    }
}
