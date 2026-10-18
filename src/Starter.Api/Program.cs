using System.Text.Json;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Scalar.AspNetCore;
using Serilog;
using Starter.Api.Errors;
using Starter.Application;
using Starter.Infrastructure;

const string ApplyMigrationsOnStartupKey = "Database:ApplyMigrationsOnStartup";
const string HealthEndpoint = "/health";

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSerilog((services, loggerConfiguration) => loggerConfiguration
    .ReadFrom.Configuration(builder.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext()
    .WriteTo.Console());

builder.Services
    .AddControllers(options => options.ModelMetadataDetailsProviders.Add(new SystemTextJsonValidationMetadataProvider()))
    .AddJsonOptions(options => options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase);
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddHealthChecks();

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    // The container is only reachable through Traefik, whose address on the Docker network is not fixed.
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

if (!builder.Environment.IsProduction())
{
    builder.Services.AddOpenApi();
}

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

app.UseForwardedHeaders();
app.UseSerilogRequestLogging();
// GlobalExceptionHandler logs with the right level itself; avoid a second Error entry per exception.
app.UseExceptionHandler(new ExceptionHandlerOptions { SuppressDiagnosticsCallback = exceptionContext => true });
app.UseStatusCodePages();

if (!app.Environment.IsProduction())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.MapControllers();
app.MapHealthChecks(HealthEndpoint);

if (app.Configuration.GetValue<bool>(ApplyMigrationsOnStartupKey))
{
    await app.Services.ApplyDatabaseMigrationsAsync(app.Lifetime.ApplicationStopping);
}

await app.RunAsync();
