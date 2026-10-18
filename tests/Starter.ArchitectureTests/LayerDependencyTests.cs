using NetArchTest.Rules;

namespace Starter.ArchitectureTests;

public sealed class LayerDependencyTests
{
    [Fact]
    public void Domain_DependsOnNoOtherLayerOrFramework()
    {
        Types.InAssembly(AssemblyReferences.Domain)
            .ShouldNot()
            .HaveDependencyOnAny(
                LayerNamespaces.Application,
                LayerNamespaces.Infrastructure,
                LayerNamespaces.Api,
                LayerNamespaces.EntityFrameworkCore,
                LayerNamespaces.AspNetCore,
                LayerNamespaces.Npgsql)
            .GetResult()
            .AssertRule("Domain must not depend on Application, Infrastructure, Api, EF Core, ASP.NET Core or Npgsql");
    }

    [Fact]
    public void Application_DependsOnNeitherOuterLayersNorFrameworks()
    {
        Types.InAssembly(AssemblyReferences.Application)
            .ShouldNot()
            .HaveDependencyOnAny(
                LayerNamespaces.Infrastructure,
                LayerNamespaces.Api,
                LayerNamespaces.EntityFrameworkCore,
                LayerNamespaces.AspNetCore,
                LayerNamespaces.Npgsql)
            .GetResult()
            .AssertRule("Application must not depend on Infrastructure, Api, EF Core, ASP.NET Core or Npgsql");
    }

    [Fact]
    public void Infrastructure_DependsOnNeitherApiNorAspNetCore()
    {
        Types.InAssembly(AssemblyReferences.Infrastructure)
            .ShouldNot()
            .HaveDependencyOnAny(LayerNamespaces.Api, LayerNamespaces.AspNetCore)
            .GetResult()
            .AssertRule("Infrastructure must not depend on Api or ASP.NET Core");
    }

    [Fact]
    public void Api_OutsideCompositionRoot_DoesNotDependOnInfrastructure()
    {
        Types.InAssembly(AssemblyReferences.Api)
            .That()
            .MeetCustomRule(new ExcludeTopLevelProgramRule())
            .ShouldNot()
            .HaveDependencyOn(LayerNamespaces.Infrastructure)
            .GetResult()
            .AssertRule("Only Program (the composition root) may depend on Infrastructure");
    }
}
