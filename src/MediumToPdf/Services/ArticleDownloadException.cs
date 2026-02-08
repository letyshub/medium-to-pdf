using System.Net;

namespace MediumToPdf.Services;

public class ArticleDownloadException : Exception
{
    public HttpStatusCode? StatusCode { get; }

    public string Url { get; }

    public ArticleDownloadException(string url, HttpStatusCode statusCode, string? message = null)
        : base(message ?? $"Failed to download article from '{url}'. HTTP {(int)statusCode} ({statusCode}).")
    {
        Url = url;
        StatusCode = statusCode;
    }

    public ArticleDownloadException(string url, string message, Exception? innerException = null)
        : base(message, innerException)
    {
        Url = url;
    }
}

public sealed class ArticleNotFoundException : ArticleDownloadException
{
    public ArticleNotFoundException(string url)
        : base(url, HttpStatusCode.NotFound, $"Article not found: '{url}'.")
    {
    }
}

public sealed class RateLimitExceededException : ArticleDownloadException
{
    public RateLimitExceededException(string url)
        : base(url, HttpStatusCode.TooManyRequests, $"Rate limit exceeded for '{url}'. Try again later.")
    {
    }
}
