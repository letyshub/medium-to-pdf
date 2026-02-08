using MediumToPdf.Commands;
using MediumToPdf.Infrastructure;
using MediumToPdf.Services;
using MediumToPdf.Tests.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console.Testing;
using Xunit;

namespace MediumToPdf.Tests.Commands;

public sealed class ConvertCommandTests
{
    private static CommandAppTester CreateApp()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IArticleDownloadService>(new MockArticleDownloadService());
        services.AddSingleton<IHtmlProcessorService>(new MockHtmlProcessorService());
        services.AddSingleton<IPdfRenderingService>(new MockPdfRenderingService());
        var registrar = new TypeRegistrar(services);
        var app = new CommandAppTester(registrar);
        app.SetDefaultCommand<ConvertCommand>();
        return app;
    }

    [Fact]
    public void Execute_WithValidArgs_ReturnsZero()
    {
        var app = CreateApp();

        var result = app.Run("https://medium.com/article", "-o", "output.pdf");

        Assert.Equal(0, result.ExitCode);
    }

    [Fact]
    public void MissingUrl_ProducesError()
    {
        var app = CreateApp();

        var result = app.Run("-o", "output.pdf");

        Assert.NotEqual(0, result.ExitCode);
    }

    [Fact]
    public void MissingOutput_ProducesError()
    {
        var app = CreateApp();

        var result = app.Run("https://medium.com/article");

        Assert.NotEqual(0, result.ExitCode);
    }

    [Fact]
    public void AllOptions_AreAccepted()
    {
        var app = CreateApp();

        var result = app.Run(
            "https://medium.com/article",
            "-o", "output.pdf",
            "--no-images",
            "--style", "custom.css");

        Assert.Equal(0, result.ExitCode);
    }
}
