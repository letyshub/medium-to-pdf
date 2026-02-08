using MediumToPdf.Models;
using MediumToPdf.Services;
using PuppeteerSharp;
using Xunit;

namespace MediumToPdf.Tests.Services;

[Trait("Category", "Integration")]
public sealed class PdfRenderingServiceTests : IAsyncLifetime
{
    private readonly BrowserManager _browserManager = new();
    private PdfRenderingService _sut = null!;
    private string _tempDir = null!;

    public Task InitializeAsync()
    {
        _sut = new PdfRenderingService(_browserManager);
        _tempDir = Path.Combine(Path.GetTempPath(), $"medium-to-pdf-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, recursive: true);
        }

        var browser = await _browserManager.GetBrowserAsync();
        await browser.DisposeAsync();
    }

    [Fact]
    public async Task RenderPdfAsync_ProducesValidPdf()
    {
        var article = CreateArticle("<p>Hello, World!</p>");
        var outputPath = Path.Combine(_tempDir, "test.pdf");

        await _sut.RenderPdfAsync(article, outputPath, customCssPath: null);

        Assert.True(File.Exists(outputPath));
        var bytes = await File.ReadAllBytesAsync(outputPath);
        Assert.True(bytes.Length >= 5);
        Assert.Equal((byte)'%', bytes[0]);
        Assert.Equal((byte)'P', bytes[1]);
        Assert.Equal((byte)'D', bytes[2]);
        Assert.Equal((byte)'F', bytes[3]);
        Assert.Equal((byte)'-', bytes[4]);
    }

    [Fact]
    public async Task RenderPdfAsync_HasReasonableFileSize()
    {
        var article = CreateArticle("<p>Minimal content</p>");
        var outputPath = Path.Combine(_tempDir, "size-test.pdf");

        await _sut.RenderPdfAsync(article, outputPath, customCssPath: null);

        var fileInfo = new FileInfo(outputPath);
        Assert.True(fileInfo.Length > 1024, $"PDF should be >1KB, was {fileInfo.Length} bytes");
    }

    [Fact]
    public async Task RenderPdfAsync_NullCustomCssPath_ProducesValidPdf()
    {
        var article = CreateArticle("<p>Default CSS only</p>");
        var outputPath = Path.Combine(_tempDir, "no-custom-css.pdf");

        await _sut.RenderPdfAsync(article, outputPath, customCssPath: null);

        Assert.True(File.Exists(outputPath));
    }

    [Fact]
    public async Task RenderPdfAsync_CustomCssPath_ProducesValidPdf()
    {
        var article = CreateArticle("<p>Custom styled</p>");
        var outputPath = Path.Combine(_tempDir, "custom-css.pdf");
        var cssPath = Path.Combine(_tempDir, "custom.css");
        await File.WriteAllTextAsync(cssPath, "body { color: red; }");

        await _sut.RenderPdfAsync(article, outputPath, customCssPath: cssPath);

        Assert.True(File.Exists(outputPath));
        var fileInfo = new FileInfo(outputPath);
        Assert.True(fileInfo.Length > 1024);
    }

    [Fact]
    public async Task RenderPdfAsync_WithArticleMetadata_ProducesValidPdf()
    {
        var article = new ArticleContent(
            Title: "Test Article",
            Author: "Jane Doe",
            PublishDate: new DateOnly(2026, 1, 15),
            BodyHtml: "<p>Content with metadata</p>");
        var outputPath = Path.Combine(_tempDir, "metadata.pdf");

        await _sut.RenderPdfAsync(article, outputPath, customCssPath: null);

        Assert.True(File.Exists(outputPath));
        var fileInfo = new FileInfo(outputPath);
        Assert.True(fileInfo.Length > 1024);
    }

    [Fact]
    public void RenderPdfAsync_CustomCssFileNotFound_ThrowsPdfGenerationFailed()
    {
        var article = CreateArticle("<p>Content</p>");
        var outputPath = Path.Combine(_tempDir, "missing-css.pdf");

        var ex = Assert.ThrowsAsync<PdfGenerationFailedException>(
            () => _sut.RenderPdfAsync(article, outputPath, "/nonexistent/style.css"));

        Assert.NotNull(ex);
    }

    private static ArticleContent CreateArticle(string bodyHtml)
    {
        return new ArticleContent(
            Title: "Test Article",
            Author: null,
            PublishDate: null,
            BodyHtml: bodyHtml);
    }
}
