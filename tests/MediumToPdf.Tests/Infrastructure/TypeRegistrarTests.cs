using MediumToPdf.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console.Cli;
using Spectre.Console.Testing;
using Xunit;

namespace MediumToPdf.Tests.Infrastructure;

public sealed class TypeRegistrarTests
{
    [Fact]
    public void RunAllBaseTests()
    {
        var baseTests = new TypeRegistrarBaseTests(() => new TypeRegistrar(new ServiceCollection()));
        baseTests.RunAllTests();
    }

    [Fact]
    public void Build_ReturnsWorkingTypeResolver()
    {
        var services = new ServiceCollection();
        var registrar = new TypeRegistrar(services);

        var resolver = registrar.Build();

        Assert.NotNull(resolver);
        Assert.IsType<TypeResolver>(resolver);
    }

    [Fact]
    public void TypeResolver_ResolvesRegisteredService()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IGreeting, HelloGreeting>();
        var registrar = new TypeRegistrar(services);

        var resolver = registrar.Build();
        var result = resolver.Resolve(typeof(IGreeting));

        Assert.NotNull(result);
        Assert.IsType<HelloGreeting>(result);
    }

    private interface IGreeting
    {
        string Greet();
    }

    private sealed class HelloGreeting : IGreeting
    {
        public string Greet() => "Hello";
    }
}
