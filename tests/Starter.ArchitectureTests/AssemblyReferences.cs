using System.Reflection;
using Starter.Application.Users.CreateUser;
using Starter.Domain.Users;
using Starter.Infrastructure;

namespace Starter.ArchitectureTests;

internal static class AssemblyReferences
{
    public static readonly Assembly Domain = typeof(User).Assembly;
    public static readonly Assembly Application = typeof(CreateUserUseCase).Assembly;
    public static readonly Assembly Infrastructure = typeof(InfrastructureServiceRegistration).Assembly;
    public static readonly Assembly Api = typeof(Program).Assembly;
}
