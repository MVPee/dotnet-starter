using NetArchTest.Rules;
using Starter.Application.Common.Persistence;

namespace Starter.ArchitectureTests;

public sealed class UnitOfWorkRulesTests
{
    private const string UseCaseSuffix = "UseCase";
    private const string SaveChangesPrefix = "SaveChanges";
    private const string UnitOfWorkTypeFullName = "Starter.Infrastructure.Persistence.UnitOfWork";
    private const int MaxSaveChangesCallSitesPerUseCase = 1;

    private static readonly string UnitOfWorkInterfaceFullName = typeof(IUnitOfWork).FullName!;
    private static readonly Type GenericRepositoryInterface = typeof(IRepository<,>);

    private static readonly string[] WritingMemberPrefixes =
        [SaveChangesPrefix, "ExecuteUpdate", "ExecuteDelete", "ExecuteSql", "BeginTransaction", "GetDbConnection"];

    [Fact]
    public void Api_DoesNotDependOnUnitOfWorkOrRepositories()
    {
        string[] persistencePorts = [UnitOfWorkInterfaceFullName, .. RepositoryInterfaceFullNames()];

        Types.InAssembly(AssemblyReferences.Api)
            .ShouldNot()
            .HaveDependencyOnAny(persistencePorts)
            .GetResult()
            .AssertRule("Api must write through use cases, never by injecting IUnitOfWork or a repository");
    }

    [Fact]
    public void Application_OnlyUseCasesDependOnUnitOfWork()
    {
        Types.InAssembly(AssemblyReferences.Application)
            .That()
            .HaveDependencyOn(UnitOfWorkInterfaceFullName)
            .GetTypes()
            .Where(dependentType => dependentType != typeof(IUnitOfWork)
                && !dependentType.Name.EndsWith(UseCaseSuffix, StringComparison.Ordinal))
            .AssertNone($"Only *{UseCaseSuffix} types may depend on {nameof(IUnitOfWork)}");
    }

    [Fact]
    public void UseCases_HaveAtMostOneSaveChangesCallSite()
    {
        var saveChangesCalls = MethodCallInspector.CallsIn(AssemblyReferences.Application)
            .Where(methodCall => methodCall.CalleeDeclaringTypeFullName == UnitOfWorkInterfaceFullName
                && methodCall.CalleeName.StartsWith(SaveChangesPrefix, StringComparison.Ordinal))
            .ToList();

        Assert.NotEmpty(saveChangesCalls);
        saveChangesCalls
            .GroupBy(methodCall => methodCall.CallerTypeFullName)
            .Where(callerGroup => callerGroup.Count() > MaxSaveChangesCallSitesPerUseCase)
            .Select(callerGroup => AssemblyReferences.Application.GetType(callerGroup.Key, throwOnError: true)!)
            .AssertNone($"A use case has at most {MaxSaveChangesCallSitesPerUseCase} SaveChanges call site: one SaveChanges is one transaction");
    }

    [Fact]
    public void Infrastructure_OnlyUnitOfWorkWritesThroughEfCore()
    {
        var writingCalls = MethodCallInspector.CallsIn(AssemblyReferences.Infrastructure)
            .Where(methodCall => WritingMemberPrefixes.Any(writingMemberPrefix => methodCall.CalleeName.StartsWith(writingMemberPrefix, StringComparison.Ordinal)))
            .ToList();
        var offendingCallers = writingCalls
            .Where(methodCall => methodCall.CallerTypeFullName != UnitOfWorkTypeFullName)
            .Select(methodCall => $"{methodCall.CallerTypeFullName} -> {methodCall.CalleeDeclaringTypeFullName}.{methodCall.CalleeName}")
            .Distinct()
            .ToList();

        Assert.Contains(writingCalls, methodCall => methodCall.CallerTypeFullName == UnitOfWorkTypeFullName);
        Assert.True(offendingCallers.Count == 0,
            $"Only {UnitOfWorkTypeFullName} may write through EF Core. Offending calls: {string.Join(", ", offendingCallers)}");
    }

    private static IEnumerable<string> RepositoryInterfaceFullNames()
        => AssemblyReferences.Application.GetTypes()
            .Where(applicationType => applicationType.IsInterface
                && (applicationType == GenericRepositoryInterface
                    || applicationType.GetInterfaces().Any(implementedInterface =>
                        implementedInterface.IsGenericType && implementedInterface.GetGenericTypeDefinition() == GenericRepositoryInterface)))
            .Select(repositoryInterface => repositoryInterface.FullName!);
}
