using System.Reflection;

namespace Angur.ArchitectureTests;

internal static class Layers
{
    public const string DomainNamespace = "Angur.Domain";
    public const string ApplicationNamespace = "Angur.Application";
    public const string InfrastructureNamespace = "Angur.Infrastructure";
    public const string ApiNamespace = "Angur.Api";

    public static readonly Assembly Domain = typeof(Domain.Abstractions.Result).Assembly;
    public static readonly Assembly Application = typeof(Application.DependencyInjection).Assembly;
    public static readonly Assembly Infrastructure = typeof(Infrastructure.DependencyInjection).Assembly;
    public static readonly Assembly Api = typeof(Program).Assembly;
}
