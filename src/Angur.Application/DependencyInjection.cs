using System.Reflection;

using Angur.Application.Abstractions.Messaging;

using Microsoft.Extensions.DependencyInjection;

namespace Angur.Application;

public static class DependencyInjection
{
    private static readonly Type[] HandlerInterfaces =
    [
        typeof(ICommandHandler<>),
        typeof(ICommandHandler<,>),
    ];

    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        Assembly assembly = typeof(DependencyInjection).Assembly;

        IEnumerable<Type> concreteClasses = assembly
            .GetTypes()
            .Where(type => type is { IsClass: true, IsAbstract: false });

        foreach (Type implementation in concreteClasses)
        {
            foreach (Type service in implementation.GetInterfaces())
            {
                if (service.IsGenericType && HandlerInterfaces.Contains(service.GetGenericTypeDefinition()))
                {
                    services.AddScoped(service, implementation);
                }
            }
        }

        return services;
    }
}
