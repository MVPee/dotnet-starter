using Mono.Cecil;
using NetArchTest.Rules;

namespace Starter.ArchitectureTests;

// Top-level statements compile to a `Program` class in the global namespace; matching by name alone would also exempt any namespaced `Program`.
internal sealed class ExcludeTopLevelProgramRule : ICustomRule
{
    private const string TopLevelProgramTypeName = "Program";

    public bool MeetsRule(TypeDefinition type)
    {
        ArgumentNullException.ThrowIfNull(type);

        var isTopLevelProgram = type.Namespace.Length == 0 && type.Name == TopLevelProgramTypeName;
        return !isTopLevelProgram;
    }
}
