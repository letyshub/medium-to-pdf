using MediumToPdf.Services;

namespace MediumToPdf.Tests.Helpers;

public sealed class MockArticleDownloadService : IArticleDownloadService
{
    private readonly string _htmlContent;

    public MockArticleDownloadService(string htmlContent = "<html><body>Mock article</body></html>")
    {
        _htmlContent = htmlContent;
    }

    public Task<string> DownloadArticleAsync(string url, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_htmlContent);
    }
}
