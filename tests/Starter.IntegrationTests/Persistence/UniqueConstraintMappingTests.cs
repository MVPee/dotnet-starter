using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Starter.Infrastructure.Persistence;
using Starter.IntegrationTests.Fixtures;

namespace Starter.IntegrationTests.Persistence;

[Collection(ApiCollectionDefinition.Name)]
public sealed class UniqueConstraintMappingTests(ApiFactory factory)
{
    [Fact]
    public async Task EveryUniqueIndexAndAlternateKey_IsMappedToADomainError()
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var model = scope.ServiceProvider.GetRequiredService<AppDbContext>().Model;

        var uniqueConstraintNames = model.GetEntityTypes()
            .SelectMany(entityType => entityType.GetIndexes()
                .Where(index => index.IsUnique)
                .Select(index => index.GetDatabaseName())
                .Concat(entityType.GetKeys()
                    .Where(key => !key.IsPrimaryKey())
                    .Select(alternateKey => alternateKey.GetName())))
            .OfType<string>()
            .ToList();
        var unmappedConstraintNames = uniqueConstraintNames
            .Where(constraintName => !UniqueConstraintErrors.TryGetError(constraintName, out _))
            .ToList();

        Assert.NotEmpty(uniqueConstraintNames);
        Assert.True(unmappedConstraintNames.Count == 0,
            $"Add these unique constraints to {nameof(UniqueConstraintErrors)}: {string.Join(", ", unmappedConstraintNames)}");
    }
}
