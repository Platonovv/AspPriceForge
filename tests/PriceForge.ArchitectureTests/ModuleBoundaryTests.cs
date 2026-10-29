using System.Reflection;
using NetArchTest.Rules;
using PriceForge.Application;
using PriceForge.Game.Api;
using PriceForge.Game.Application;
using PriceForge.Game.Domain.Wallets;
using PriceForge.Game.Infrastructure;
using PriceForge.Infrastructure;

namespace PriceForge.ArchitectureTests;

[Trait("Category", "Unit")]
public class ModuleBoundaryTests
{
    private static readonly string[] CoreNamespaces =
    [
        "PriceForge.Domain",
        "PriceForge.Application",
        "PriceForge.Infrastructure",
        "PriceForge.Api"
    ];

    private static readonly string[] GameInternalNamespaces =
    [
        "PriceForge.Game.Domain",
        "PriceForge.Game.Application",
        "PriceForge.Game.Infrastructure",
        "PriceForge.Game.Api"
    ];

    public static TheoryData<string> GameAssemblies => new()
    {
        typeof(Wallet).Assembly.GetName().Name!,
        typeof(GameApplicationDependencyInjection).Assembly.GetName().Name!,
        typeof(GameInfrastructureDependencyInjection).Assembly.GetName().Name!,
        typeof(GameModule).Assembly.GetName().Name!
    };

    public static TheoryData<string> CoreAssemblies => new()
    {
        typeof(PriceForge.Domain.Products.Product).Assembly.GetName().Name!,
        typeof(ApplicationDependencyInjection).Assembly.GetName().Name!,
        typeof(InfrastructureDependencyInjection).Assembly.GetName().Name!
    };

    [Theory]
    [MemberData(nameof(GameAssemblies))]
    public void GameModule_DoesNotDependOnCore(string assemblyName)
    {
        TestResult result = Types.InAssembly(Assembly.Load(assemblyName))
                                 .ShouldNot()
                                 .HaveDependencyOnAny(CoreNamespaces)
                                 .GetResult();

        Assert.True(result.IsSuccessful, Describe(result));
    }

    [Theory]
    [MemberData(nameof(CoreAssemblies))]
    public void Core_DoesNotDependOnGameInternals(string assemblyName)
    {
        TestResult result = Types.InAssembly(Assembly.Load(assemblyName))
                                 .ShouldNot()
                                 .HaveDependencyOnAny(GameInternalNamespaces)
                                 .GetResult();

        Assert.True(result.IsSuccessful, Describe(result));
    }

    [Fact]
    public void GameDomain_DependsOnlyOnBuildingBlocks()
    {
        TestResult result = Types.InAssembly(typeof(Wallet).Assembly)
                                 .ShouldNot()
                                 .HaveDependencyOnAny("PriceForge.Game.Application",
                                                      "PriceForge.Game.Infrastructure",
                                                      "PriceForge.Game.Api",
                                                      "Microsoft.EntityFrameworkCore",
                                                      "MediatR")
                                 .GetResult();

        Assert.True(result.IsSuccessful, Describe(result));
    }

    private static string Describe(TestResult result)
        => "Violations: " + string.Join(", ", result.FailingTypeNames ?? []);
}
