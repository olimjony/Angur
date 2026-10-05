using NetArchTest.Rules;

using TestResult = NetArchTest.Rules.TestResult;

namespace Angur.ArchitectureTests;

public class LayerDependencyTests
{
    [Fact]
    public void Domain_ShouldNotDependOnOtherLayers()
    {
        // Act
        TestResult result = Types.InAssembly(Layers.Domain)
            .ShouldNot()
            .HaveDependencyOnAny(
                Layers.ApplicationNamespace,
                Layers.InfrastructureNamespace,
                Layers.ApiNamespace)
            .GetResult();

        // Assert
        AssertSuccessful(result);
    }

    [Fact]
    public void Application_ShouldNotDependOnInfrastructureOrApi()
    {
        TestResult result = Types.InAssembly(Layers.Application)
            .ShouldNot()
            .HaveDependencyOnAny(
                Layers.InfrastructureNamespace,
                Layers.ApiNamespace)
            .GetResult();

        AssertSuccessful(result);
    }

    [Fact]
    public void Infrastructure_ShouldNotDependOnApi()
    {
        TestResult result = Types.InAssembly(Layers.Infrastructure)
            .ShouldNot()
            .HaveDependencyOnAny(Layers.ApiNamespace)
            .GetResult();

        AssertSuccessful(result);
    }

    [Fact]
    public void Api_OnlyProgramMayUseInfrastructure()
    {
        TestResult result = Types.InAssembly(Layers.Api)
            .That()
            .DoNotHaveName("Program")
            .ShouldNot()
            .HaveDependencyOnAny(Layers.InfrastructureNamespace)
            .GetResult();

        AssertSuccessful(result);
    }

    internal static void AssertSuccessful(TestResult result)
    {
        IEnumerable<string> failingTypes = result.FailingTypes?.Select(t => t.FullName) ?? [];

        Assert.True(
            result.IsSuccessful,
            $"Architecture rule violated by: {string.Join(", ", failingTypes)}");
    }
}
