namespace MediumToPdf.Services;

public interface IArticleDownloadService
{
    Task<string> DownloadArticleAsync(string url, CancellationToken cancellationToken = default);
}
