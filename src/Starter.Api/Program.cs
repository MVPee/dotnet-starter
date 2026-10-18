using System.Text.Json;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Scalar.AspNetCore;
using Serilog;
using Starter.Api.Errors;
using Starter.Api.Hosting;
using Starter.Api.Networking;
using Starter.Api.RateLimiting;
using Starter.Application;
using Starter.Infrastructure;

const string ApplyMigrationsOnStartupKey = "Database:ApplyMigrationsOnStartup";
const string HealthEndpoint = "/health";
const string ClientIpProperty = "ClientIp";
const string RequestLogTemplate = "HTTP {RequestMethod} {RequestPath} from {ClientIp} responded {StatusCode} in {Elapsed:0.0000} ms";

InitProcessCrashExit.Register();

var builder = WebApplication.CreateBuilder(args);
AllowedHostsGuard.EnsureRestrictedOutsideDevelopment(builder.Configuration, builder.Environment);

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

builder.Services.AddTrustedProxyForwardedHeaders(builder.Configuration, builder.Environment);
builder.Services.AddClientRateLimiting(builder.Configuration);

if (!builder.Environment.IsProduction())
{
    builder.Services.AddOpenApi();
}

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

app.UseForwardedHeaders();
app.UseSerilogRequestLogging(options =>
{
    options.MessageTemplate = RequestLogTemplate;
    options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
        diagnosticContext.Set(ClientIpProperty, httpContext.Connection.RemoteIpAddress?.ToString());
});
// GlobalExceptionHandler logs with the right level itself; avoid a second Error entry per exception.
app.UseExceptionHandler(new ExceptionHandlerOptions { SuppressDiagnosticsCallback = _ => true });
app.UseStatusCodePages();
app.UseRateLimiter();

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
