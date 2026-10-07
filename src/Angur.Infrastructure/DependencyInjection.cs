using Angur.Application.Abstractions;
using Angur.Infrastructure.Accounts;
using Angur.Infrastructure.Database;
using Angur.Infrastructure.Repositories;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Angur.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSingleton(TimeProvider.System);

        string connectionString = configuration.GetConnectionString("Database")
        ?? throw new InvalidOperationException("Connection string 'Database' is not configured.");

        services.AddDbContext<AngurDbContext>(options => options
            .UseNpgsql(connectionString)
            .UseSnakeCaseNamingConvention());

        services.AddScoped<IUnitOfWork>(provider => provider.GetRequiredService<AngurDbContext>());

        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<IAccountRepository, AccountRepository>();
        services.AddScoped<IAccountNumberGenerator, AccountNumberGenerator>();

        return services;
    }
}
