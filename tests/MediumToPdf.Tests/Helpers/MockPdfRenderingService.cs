using MediumToPdf.Models;
using MediumToPdf.Services;

namespace MediumToPdf.Tests.Helpers;

public sealed class MockPdfRenderingService : IPdfRenderingService
{
    private readonly PdfRenderingException? _exception;

    public ArticleContent? LastArticle { get; private set; }
    public string? LastOutputPath { get; private set; }
    public string? LastCustomCssPath { get; private set; }

    public MockPdfRenderingService()
    {
    }

    public MockPdfRenderingService(PdfRenderingException exception)
    {
        _exception = exception;
    }

    public Task RenderPdfAsync(
        ArticleContent article,
        string outputPath,
        string? customCssPath,
        CancellationToken cancellationToken = default)
    {
        if (_exception is not null)
        {
            throw _exception;
        }

        LastArticle = article;
        LastOutputPath = outputPath;
        LastCustomCssPath = customCssPath;
        return Task.CompletedTask;
    }
}
