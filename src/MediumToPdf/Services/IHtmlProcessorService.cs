using MediumToPdf.Models;

namespace MediumToPdf.Services;

public interface IHtmlProcessorService
{
    Task<ArticleContent> ExtractArticleAsync(string html, CancellationToken cancellationToken = default);
}
