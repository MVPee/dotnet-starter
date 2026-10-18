using Microsoft.Extensions.DependencyInjection;
using Starter.Application.Users.CreateUser;
using Starter.Application.Users.GetUserById;
using Starter.Application.Users.GetUsers;

namespace Starter.Application;

public static class ApplicationServiceRegistration
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<CreateUserUseCase>();
        services.AddScoped<GetUserByIdUseCase>();
        services.AddScoped<GetUsersUseCase>();
        return services;
    }
}
