using System.Reflection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.RateLimiting;
using Starter.Api.RateLimiting;

namespace Starter.ArchitectureTests;

public sealed class RateLimitingRulesTests
{
    private const BindingFlags PublicInstanceMethods = BindingFlags.Public | BindingFlags.Instance;

    private static readonly HashSet<string> WritingHttpMethods =
        new([HttpMethods.Post, HttpMethods.Put, HttpMethods.Patch, HttpMethods.Delete], StringComparer.OrdinalIgnoreCase);

    [Fact]
    public void Actions_DeclareThePolicyMatchingTheirHttpMethod()
    {
        var actions = ControllerActions();
        var actionsWithWrongPolicy = actions
            .Where(controllerAction => controllerAction.Action.GetCustomAttribute<EnableRateLimitingAttribute>()?.PolicyName
                != ExpectedPolicy(controllerAction.Action))
            .Select(controllerAction => $"{controllerAction.ControllerName}.{controllerAction.Action.Name} (expected '{ExpectedPolicy(controllerAction.Action)}')")
            .ToList();

        Assert.NotEmpty(actions);
        Assert.True(actionsWithWrongPolicy.Count == 0,
            $"Each action needs [EnableRateLimiting]: {nameof(RateLimitPolicies.Read)} for GET, {nameof(RateLimitPolicies.Write)} for POST/PUT/PATCH/DELETE. Offending: {string.Join(", ", actionsWithWrongPolicy)}");
    }

    [Fact]
    public void Actions_NeverMixReadAndWriteVerbs()
    {
        var mixedVerbActions = ControllerActions()
            .Where(controllerAction => HttpMethodsOf(controllerAction.Action).Any(WritingHttpMethods.Contains)
                && HttpMethodsOf(controllerAction.Action).Any(httpMethod => !WritingHttpMethods.Contains(httpMethod)))
            .Select(controllerAction => $"{controllerAction.ControllerName}.{controllerAction.Action.Name}")
            .ToList();

        Assert.True(mixedVerbActions.Count == 0, $"An action is either a read or a write, so its rate limit policy is unambiguous. Offending: {string.Join(", ", mixedVerbActions)}");
    }

    [Fact]
    public void Controllers_DeclarePoliciesOnActionsOnly()
    {
        ControllerTypes()
            .Where(controllerType => controllerType.GetCustomAttribute<EnableRateLimitingAttribute>() is not null
                || controllerType.GetCustomAttribute<DisableRateLimitingAttribute>() is not null)
            .AssertNone("Rate limiting attributes belong on actions, not on the controller class");
    }

    [Fact]
    public void Actions_NeverDisableRateLimiting()
    {
        var actionsWithoutLimit = ControllerActions()
            .Where(controllerAction => controllerAction.Action.GetCustomAttribute<DisableRateLimitingAttribute>() is not null)
            .Select(controllerAction => $"{controllerAction.ControllerName}.{controllerAction.Action.Name}")
            .ToList();

        Assert.True(actionsWithoutLimit.Count == 0, $"[DisableRateLimiting] is not allowed. Offending: {string.Join(", ", actionsWithoutLimit)}");
    }

    private static string ExpectedPolicy(MethodInfo action)
        => HttpMethodsOf(action).Any(WritingHttpMethods.Contains) ? RateLimitPolicies.Write : RateLimitPolicies.Read;

    private static List<string> HttpMethodsOf(MethodInfo action)
        => [.. action.GetCustomAttributes().OfType<IActionHttpMethodProvider>().SelectMany(methodProvider => methodProvider.HttpMethods)];

    private static List<(string ControllerName, MethodInfo Action)> ControllerActions()
        => [.. ControllerTypes().SelectMany(controllerType => controllerType.GetMethods(PublicInstanceMethods)
            .Where(method => method.DeclaringType != typeof(ControllerBase)
                && method.DeclaringType != typeof(object)
                && method.GetCustomAttribute<NonActionAttribute>() is null
                && HttpMethodsOf(method).Count > 0)
            .Select(action => (controllerType.Name, action)))];

    private static List<Type> ControllerTypes()
        => [.. AssemblyReferences.Api.GetTypes().Where(apiType => apiType is { IsClass: true, IsAbstract: false } && typeof(ControllerBase).IsAssignableFrom(apiType))];
}
