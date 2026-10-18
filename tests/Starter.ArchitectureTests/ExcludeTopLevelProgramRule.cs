using Mono.Cecil;
using NetArchTest.Rules;

namespace Starter.ArchitectureTests;

// Also checks the global namespace so a namespaced `Program` is not exempted.
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
