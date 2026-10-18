using Microsoft.Extensions.DependencyInjection;
using Starter.Application;
using Starter.IntegrationTests.Fixtures;

namespace Starter.IntegrationTests.Composition;

[Collection(ApiCollectionDefinition.Name)]
public sealed class DependencyInjectionTests(ApiFactory factory)
{
    private const string UseCaseSuffix = "UseCase";

    public static TheoryData<Type> UseCaseTypes()
    {
        var useCaseTypes = new TheoryData<Type>();
        foreach (var useCaseType in typeof(ApplicationServiceRegistration).Assembly.GetExportedTypes()
            .Where(exportedType => exportedType.IsClass && !exportedType.IsAbstract && exportedType.Name.EndsWith(UseCaseSuffix, StringComparison.Ordinal)))
        {
            useCaseTypes.Add(useCaseType);
        }

        return useCaseTypes;
    }

    [Theory]
    [MemberData(nameof(UseCaseTypes))]
    public void Container_ForEveryUseCase_ResolvesWithAllDependencies(Type useCaseType)
    {
        using var scope = factory.Services.CreateScope();

        var useCase = scope.ServiceProvider.GetService(useCaseType);

        Assert.NotNull(useCase);
    }
}
