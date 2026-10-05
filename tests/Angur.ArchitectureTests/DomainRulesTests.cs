using NetArchTest.Rules;

using TestResult = NetArchTest.Rules.TestResult;

namespace Angur.ArchitectureTests;

public class DomainRulesTests
{
    [Fact]
    public void Domain_ShouldNotDependOnFrameworks()
    {
        TestResult result = Types.InAssembly(Layers.Domain)
            .ShouldNot()
            .HaveDependencyOnAny(
                "Microsoft.EntityFrameworkCore",
                "Microsoft.AspNetCore",
                "Microsoft.Extensions.DependencyInjection",
                "System.Data")
            .GetResult();

        LayerDependencyTests.AssertSuccessful(result);
    }

    [Fact]
    public void Application_ShouldNotDependOnWebOrDatabaseFrameworks()
    {
        TestResult result = Types.InAssembly(Layers.Application)
            .ShouldNot()
            .HaveDependencyOnAny(
                "Microsoft.EntityFrameworkCore",
                "Microsoft.AspNetCore")
            .GetResult();

        LayerDependencyTests.AssertSuccessful(result);
    }

    [Fact]
    public void DomainConcreteClasses_ShouldBeSealed()
    {
        TestResult result = Types.InAssembly(Layers.Domain)
            .That()
            .AreClasses()
            .And()
            .AreNotAbstract()
            .And()
            .AreNotStatic()
            .And()
            .DoNotResideInNamespace($"{Layers.DomainNamespace}.Abstractions")
            .Should()
            .BeSealed()
            .GetResult();

        LayerDependencyTests.AssertSuccessful(result);
    }
}
