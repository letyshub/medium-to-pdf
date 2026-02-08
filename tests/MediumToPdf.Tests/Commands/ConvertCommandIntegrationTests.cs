using MediumToPdf.Commands;
using MediumToPdf.Infrastructure;
using MediumToPdf.Models;
using MediumToPdf.Services;
using MediumToPdf.Tests.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console.Testing;
using Xunit;

namespace MediumToPdf.Tests.Commands;

public sealed class ConvertCommandIntegrationTests
{
    private static CommandAppTester CreateApp(
        MockArticleDownloadService? downloadService = null,
        MockHtmlProcessorService? htmlProcessor = null,
        MockPdfRenderingService? pdfRenderer = null)
    {
        var services = new ServiceCollection();
        services.AddSingleton<IArticleDownloadService>(downloadService ?? new MockArticleDownloadService());
        services.AddSingleton<IHtmlProcessorService>(htmlProcessor ?? new MockHtmlProcessorService());
        services.AddSingleton<IPdfRenderingService>(pdfRenderer ?? new MockPdfRenderingService());
        var registrar = new TypeRegistrar(services);
        var app = new CommandAppTester(registrar);
        app.SetDefaultCommand<ConvertCommand>();
        return app;
    }

    [Fact]
    public void Execute_WithBothServices_ReturnsZero()
    {
        var app = CreateApp();

        var result = app.Run("https://medium.com/article", "-o", "output.pdf");

        Assert.Equal(0, result.ExitCode);
    }

    [Fact]
    public void Execute_HtmlProcessingException_ReturnsOne()
    {
        var mock = new MockHtmlProcessorService(
            new HtmlProcessingException("No title found", "<html></html>"));
        var app = CreateApp(htmlProcessor: mock);

        var result = app.Run("https://medium.com/article", "-o", "output.pdf");

        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public void Execute_WithFullMetadata_ReturnsZero()
    {
        var content = new ArticleContent(
            Title: "Test Article",
            Author: "Test Author",
            PublishDate: new DateOnly(2026, 1, 15),
            BodyHtml: "<p>Content</p>");
        var mock = new MockHtmlProcessorService(content);
        var app = CreateApp(htmlProcessor: mock);

        var result = app.Run("https://medium.com/article", "-o", "output.pdf");

        Assert.Equal(0, result.ExitCode);
    }

    [Fact]
    public void Execute_NullAuthorAndDate_ReturnsZero()
    {
        var content = new ArticleContent(
            Title: "Title Only",
            Author: null,
            PublishDate: null,
            BodyHtml: "<p>Content</p>");
        var mock = new MockHtmlProcessorService(content);
        var app = CreateApp(htmlProcessor: mock);

        var result = app.Run("https://medium.com/article", "-o", "output.pdf");

        Assert.Equal(0, result.ExitCode);
    }

    [Fact]
    public void Execute_PdfGenerationFailedException_ReturnsOne()
    {
        var mock = new MockPdfRenderingService(
            new PdfGenerationFailedException("Disk full", "/tmp/output.pdf"));
        var app = CreateApp(pdfRenderer: mock);

        var result = app.Run("https://medium.com/article", "-o", "output.pdf");

        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public void Execute_BrowserLaunchException_ReturnsOne()
    {
        var mock = new MockPdfRenderingService(
            new BrowserLaunchException("Chrome not found"));
        var app = CreateApp(pdfRenderer: mock);

        var result = app.Run("https://medium.com/article", "-o", "output.pdf");

        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public void Execute_PassesStyleAsCssPath()
    {
        var mock = new MockPdfRenderingService();
        var app = CreateApp(pdfRenderer: mock);

        var result = app.Run(
            "https://medium.com/article",
            "-o", "output.pdf",
            "--style", "custom.css");

        Assert.Equal(0, result.ExitCode);
        Assert.Equal("custom.css", mock.LastCustomCssPath);
    }

    [Fact]
    public void Execute_NoStyle_NullCssPath()
    {
        var mock = new MockPdfRenderingService();
        var app = CreateApp(pdfRenderer: mock);

        var result = app.Run("https://medium.com/article", "-o", "output.pdf");

        Assert.Equal(0, result.ExitCode);
        Assert.Null(mock.LastCustomCssPath);
    }
}
