using System.Reflection;
using System.Runtime.CompilerServices;
using Starter.Application.Common.Persistence;

namespace Starter.ArchitectureTests;

public sealed class RepositoryRulesTests
{
    private const string RepositoryBaseTypeName = "Starter.Infrastructure.Persistence.Repository`3";
    private const string DbContextTypeName = "Starter.Infrastructure.Persistence.AppDbContext";
    private const string UnitOfWorkTypeName = "Starter.Infrastructure.Persistence.UnitOfWork";
    private const string RepositoryInterfacePrefix = "I";
    private const string RepositorySuffix = "Repository";
    private const BindingFlags DeclaredMembers =
        BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

    private static readonly Type GenericRepositoryInterface = typeof(IRepository<,>);
    private static readonly string[] AllowedRepositoryOverrides = ["ApplyFilter", "ToModel", "ToEntity"];

    [Fact]
    public void Repositories_Exist()
    {
        Assert.NotEmpty(ConcreteRepositoryTypes());
    }

    [Fact]
    public void Repositories_InheritGenericRepositoryBase()
    {
        var repositoryBaseType = AssemblyReferences.Infrastructure.GetType(RepositoryBaseTypeName, throwOnError: true)!;

        ConcreteRepositoryTypes()
            .Where(repositoryType => !InheritsGenericDefinition(repositoryType, repositoryBaseType))
            .AssertNone($"Repositories must inherit {repositoryBaseType.Name} instead of implementing CRUD by hand");
    }

    [Fact]
    public void Repositories_AreSealed()
    {
        ConcreteRepositoryTypes()
            .Where(repositoryType => !repositoryType.IsSealed)
            .AssertNone("Repositories must be sealed so no intermediate class can add custom queries");
    }

    [Fact]
    public void Repositories_DeclareOnlyFilterAndMappingOverrides()
    {
        var repositoryBaseType = AssemblyReferences.Infrastructure.GetType(RepositoryBaseTypeName, throwOnError: true)!;

        ConcreteRepositoryTypes()
            .Where(repositoryType => repositoryType.GetMethods(DeclaredMembers)
                .Any(declaredMethod => !declaredMethod.IsSpecialName
                    && !declaredMethod.IsDefined(typeof(CompilerGeneratedAttribute))
                    && !IsAllowedOverride(declaredMethod, repositoryBaseType)))
            .AssertNone($"Repositories may only override {string.Join(", ", AllowedRepositoryOverrides)}; no custom queries, no SaveChanges");
    }

    [Fact]
    public void DbContext_IsInjectedOnlyIntoRepositoriesAndUnitOfWork()
    {
        var dbContextType = AssemblyReferences.Infrastructure.GetType(DbContextTypeName, throwOnError: true)!;
        var repositoryBaseType = AssemblyReferences.Infrastructure.GetType(RepositoryBaseTypeName, throwOnError: true)!;

        AssemblyReferences.Infrastructure.GetTypes()
            .Where(infrastructureType => infrastructureType.GetConstructors(DeclaredMembers)
                .Any(constructor => constructor.GetParameters().Any(parameter => parameter.ParameterType == dbContextType)))
            .Where(consumerType => consumerType.FullName != UnitOfWorkTypeName
                && consumerType != repositoryBaseType
                && !InheritsGenericDefinition(consumerType, repositoryBaseType))
            .AssertNone($"Only repositories and {UnitOfWorkTypeName} may depend on {dbContextType.Name}; everything else goes through them");
    }

    [Fact]
    public void RepositoryInterfaces_ExtendGenericRepositoryWithoutExtraMembers()
    {
        var repositoryInterfaces = AssemblyReferences.Application.GetTypes()
            .Where(applicationType => applicationType.IsInterface
                && applicationType.Name.StartsWith(RepositoryInterfacePrefix, StringComparison.Ordinal)
                && applicationType.Name.EndsWith(RepositorySuffix, StringComparison.Ordinal))
            .ToList();

        Assert.NotEmpty(repositoryInterfaces);
        repositoryInterfaces
            .Where(repositoryInterface => !ImplementsGenericRepository(repositoryInterface)
                || repositoryInterface.GetMembers(DeclaredMembers).Length > 0)
            .AssertNone($"I*{RepositorySuffix} interfaces must extend {GenericRepositoryInterface.Name} and declare nothing else");
    }

    private static List<Type> ConcreteRepositoryTypes()
        => AssemblyReferences.Infrastructure.GetTypes()
            .Where(infrastructureType => infrastructureType is { IsClass: true, IsAbstract: false } && ImplementsGenericRepository(infrastructureType))
            .ToList();

    private static bool ImplementsGenericRepository(Type candidateType)
        => candidateType.GetInterfaces().Any(implementedInterface =>
            implementedInterface.IsGenericType && implementedInterface.GetGenericTypeDefinition() == GenericRepositoryInterface);

    private static bool IsAllowedOverride(MethodInfo declaredMethod, Type repositoryBaseType)
    {
        var baseDefinitionType = declaredMethod.GetBaseDefinition().DeclaringType;
        return AllowedRepositoryOverrides.Contains(declaredMethod.Name)
            && baseDefinitionType is { IsGenericType: true }
            && baseDefinitionType.GetGenericTypeDefinition() == repositoryBaseType;
    }

    private static bool InheritsGenericDefinition(Type candidateType, Type genericDefinition)
    {
        for (var baseType = candidateType.BaseType; baseType is not null; baseType = baseType.BaseType)
        {
            if (baseType.IsGenericType && baseType.GetGenericTypeDefinition() == genericDefinition)
            {
                return true;
            }
        }

        return false;
    }
}
