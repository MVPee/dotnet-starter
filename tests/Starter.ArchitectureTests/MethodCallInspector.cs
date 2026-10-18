using System.Reflection;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Starter.ArchitectureTests;

internal sealed record MethodCall(string CallerTypeFullName, string CalleeDeclaringTypeFullName, string CalleeName);

internal static class MethodCallInspector
{
    // Ldftn/Ldvirtftn catch method groups like `var save = unitOfWork.SaveChangesAsync;`.
    private static readonly HashSet<OpCode> MethodReferencingOpCodes = [OpCodes.Call, OpCodes.Callvirt, OpCodes.Ldftn, OpCodes.Ldvirtftn];

    public static IReadOnlyList<MethodCall> CallsIn(Assembly assembly)
    {
        using var module = ModuleDefinition.ReadModule(assembly.Location);
        return [.. module.Types.SelectMany(topLevelType => SelfAndNestedTypes(topLevelType)
            .SelectMany(declaringType => declaringType.Methods)
            .Where(method => method.HasBody)
            .SelectMany(method => method.Body.Instructions)
            .Where(instruction => MethodReferencingOpCodes.Contains(instruction.OpCode))
            .Select(instruction => (MethodReference)instruction.Operand)
            .Select(callee => new MethodCall(topLevelType.FullName, callee.DeclaringType.GetElementType().FullName, callee.Name)))];
    }

    private static IEnumerable<TypeDefinition> SelfAndNestedTypes(TypeDefinition type)
        => type.NestedTypes.SelectMany(SelfAndNestedTypes).Prepend(type);
}
