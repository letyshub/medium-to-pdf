using MediumToPdf.Models;
using MediumToPdf.Services;

namespace MediumToPdf.Tests.Helpers;

public sealed class MockHtmlProcessorService : IHtmlProcessorService
{
    private readonly ArticleContent? _result;
    private readonly HtmlProcessingException? _exception;

    public MockHtmlProcessorService(ArticleContent? result = null)
    {
        _result = result ?? new ArticleContent(
            Title: "Mock Title",
            Author: "Mock Author",
            PublishDate: new DateOnly(2026, 1, 1),
            BodyHtml: "<p>Mock body</p>");
    }

    public MockHtmlProcessorService(HtmlProcessingException exception)
    {
        _exception = exception;
    }

    public Task<ArticleContent> ExtractArticleAsync(
        string html,
        CancellationToken cancellationToken = default)
    {
        if (_exception is not null)
        {
            throw _exception;
        }

        return Task.FromResult(_result!);
    }
}
