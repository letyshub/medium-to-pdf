using MediumToPdf.Models;

namespace MediumToPdf.Services;

public interface IPdfRenderingService
{
    Task RenderPdfAsync(
        ArticleContent article,
        string outputPath,
        string? customCssPath,
        CancellationToken cancellationToken = default);
}
