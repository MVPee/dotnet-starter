using NetArchTest.Rules;

namespace Starter.ArchitectureTests;

public sealed class TypePlacementTests
{
    private const string ServiceRegistrationSuffix = "ServiceRegistration";
    private const string EntitySuffix = "Entity";
    private const string DtoSuffix = "Dto";
    private const string UseCaseSuffix = "UseCase";

    [Fact]
    public void Infrastructure_PublicTypes_AreOnlyServiceRegistrations()
    {
        Types.InAssembly(AssemblyReferences.Infrastructure)
            .That()
            .ArePublic()
            .Should()
            .BeClasses()
            .And()
            .HaveNameEndingWith(ServiceRegistrationSuffix)
            .GetResult()
            .AssertRule($"Infrastructure must keep everything internal except *{ServiceRegistrationSuffix} classes");
    }

    [Fact]
    public void EntityTypes_ResideOnlyInInfrastructure()
    {
        Types.InAssemblies([AssemblyReferences.Domain, AssemblyReferences.Application, AssemblyReferences.Api])
            .That()
            .HaveNameEndingWith(EntitySuffix)
            .GetTypes()
            .AssertNone($"*{EntitySuffix} types are persistence models and belong in Infrastructure only");
    }

    [Fact]
    public void DtoTypes_ResideOnlyInApi()
    {
        Types.InAssemblies([AssemblyReferences.Domain, AssemblyReferences.Application, AssemblyReferences.Infrastructure])
            .That()
            .HaveNameEndingWith(DtoSuffix)
            .GetTypes()
            .AssertNone($"*{DtoSuffix} types are HTTP contracts and belong in Api only");
    }

    [Fact]
    public void UseCaseTypes_ResideOnlyInApplication()
    {
        Types.InAssemblies([AssemblyReferences.Domain, AssemblyReferences.Infrastructure, AssemblyReferences.Api])
            .That()
            .HaveNameEndingWith(UseCaseSuffix)
            .GetTypes()
            .AssertNone($"*{UseCaseSuffix} types belong in Application only");
    }

    [Fact]
    public void UseCaseTypes_AreSealedClasses()
    {
        Types.InAssembly(AssemblyReferences.Application)
            .That()
            .HaveNameEndingWith(UseCaseSuffix)
            .Should()
            .BeClasses()
            .And()
            .BeSealed()
            .GetResult()
            .AssertRule($"*{UseCaseSuffix} types must be sealed classes");
    }

    // Guards the suffix-based rules above against passing vacuously after a rename.
    [Fact]
    public void UseCaseTypes_Exist()
    {
        var useCaseTypes = Types.InAssembly(AssemblyReferences.Application)
            .That()
            .HaveNameEndingWith(UseCaseSuffix)
            .GetTypes();

        Assert.NotEmpty(useCaseTypes);
    }
}
