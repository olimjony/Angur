using System.Reflection;

using Angur.Application.Abstractions;
using Angur.Application.Abstractions.Messaging;
using Angur.Application.UnitTests.Fakes;

using Microsoft.Extensions.DependencyInjection;

using static Angur.Application.UnitTests.TestData;

namespace Angur.Application.UnitTests;

public class DependencyInjectionTests
{
    private static readonly Assembly ApplicationAssembly = typeof(DependencyInjection).Assembly;

    [Fact]
    public void AddApplication_RegistersExactlyOneHandlerForEveryCommand()
    {
        // We do NOT list the handlers by hand: we find every command and check it has a handler.
        // So a new command without a handler (or with a mistyped interface) fails this test.
        IServiceCollection services = new ServiceCollection().AddApplication();
        List<Type> commands = FindCommands();

        List<string> problems = [];
        foreach (Type command in commands)
        {
            Type handlerService = ExpectedHandlerService(command);
            int registrations = services.Count(descriptor => descriptor.ServiceType == handlerService);

            if (registrations != 1)
            {
                problems.Add($"{command.Name}: {registrations} handler(s) registered");
            }
        }

        Assert.NotEmpty(commands); // otherwise the loop above proves nothing
        Assert.Empty(problems);
    }

    [Fact]
    public void AddApplication_RegistersHandlersAsScoped()
    {
        // Scoped = one instance per HTTP request, the same lifetime as the DbContext they will use.
        // A Singleton handler would capture one DbContext forever (captive dependency).
        IServiceCollection services = new ServiceCollection().AddApplication();

        Assert.NotEmpty(services);
        Assert.All(services, descriptor => Assert.Equal(ServiceLifetime.Scoped, descriptor.Lifetime));
    }

    [Fact]
    public void AddApplication_EveryHandlerCanBeCreatedWhenPortsAreRegistered()
    {
        // Arrange: what Infrastructure will provide later, replaced by fakes.
        IServiceCollection services = new ServiceCollection()
            .AddApplication()
            .AddScoped<ICustomerRepository, FakeCustomerRepository>()
            .AddScoped<IAccountRepository, FakeAccountRepository>()
            .AddScoped<IUnitOfWork, FakeUnitOfWork>()
            .AddScoped<IAccountNumberGenerator, FakeAccountNumberGenerator>()
            .AddSingleton<TimeProvider>(new FakeTimeProvider(Now));

        // ValidateOnBuild: the container tries to build every registration up front
        // and throws if some constructor needs a service nobody registered.
        using ServiceProvider provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true,
        });

        // Act + Assert: resolve every handler inside a scope, like a real request would.
        using IServiceScope scope = provider.CreateScope();
        foreach (Type command in FindCommands())
        {
            object? handler = scope.ServiceProvider.GetService(ExpectedHandlerService(command));
            Assert.NotNull(handler);
        }
    }

    // ---------- Helpers ----------

    private static List<Type> FindCommands() =>
        [.. ApplicationAssembly
            .GetTypes()
            .Where(type => type is { IsClass: true, IsAbstract: false })
            .Where(type => type.GetInterfaces().Any(IsCommandInterface))];

    private static bool IsCommandInterface(Type type) =>
        type == typeof(ICommand)
        || (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ICommand<>));

    /// <summary>ICommand → ICommandHandler&lt;TCommand&gt;; ICommand&lt;TResponse&gt; → ICommandHandler&lt;TCommand, TResponse&gt;.</summary>
    private static Type ExpectedHandlerService(Type command)
    {
        Type commandInterface = command.GetInterfaces().First(IsCommandInterface);

        return commandInterface == typeof(ICommand)
            ? typeof(ICommandHandler<>).MakeGenericType(command)
            : typeof(ICommandHandler<,>).MakeGenericType(command, commandInterface.GetGenericArguments()[0]);
    }
}
