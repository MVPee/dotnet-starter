using System.Reflection;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Starter.ArchitectureTests;

internal sealed record MethodCall(string CallerTypeFullName, string CalleeDeclaringTypeFullName, string CalleeName);

// Reads compiled IL so rules see calls inside async state machines and lambdas, which compile to nested types.
internal static class MethodCallInspector
{
    // Ldftn/Ldvirtftn load a method into a delegate (`var save = unitOfWork.SaveChangesAsync;`), which would otherwise hide the call.
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
