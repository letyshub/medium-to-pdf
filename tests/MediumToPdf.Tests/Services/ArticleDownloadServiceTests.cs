using System.Net;
using MediumToPdf.Services;
using MediumToPdf.Tests.Helpers;
using Xunit;

namespace MediumToPdf.Tests.Services;

public sealed class ArticleDownloadServiceTests
{
    private static HttpClient CreateHttpClient(
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler)
    {
        var mockHandler = new MockHttpMessageHandler(handler);
        return new HttpClient(mockHandler);
    }

    [Fact]
    public async Task DownloadArticleAsync_Success_ReturnsHtmlContent()
    {
        var expectedHtml = "<html><body>Article content</body></html>";
        var httpClient = CreateHttpClient((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(expectedHtml),
            }));

        var service = new ArticleDownloadService(httpClient, delayMs: 0);

        var result = await service.DownloadArticleAsync("https://medium.com/article");

        Assert.Equal(expectedHtml, result);
    }

    [Fact]
    public async Task DownloadArticleAsync_SetsUserAgentHeader()
    {
        string? capturedUserAgent = null;
        var httpClient = CreateHttpClient((request, _) =>
        {
            capturedUserAgent = request.Headers.UserAgent.ToString();
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("<html></html>"),
            });
        });
        httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(ArticleDownloadService.GetUserAgent());

        var service = new ArticleDownloadService(httpClient, delayMs: 0);
        await service.DownloadArticleAsync("https://medium.com/article");

        Assert.NotNull(capturedUserAgent);
        Assert.StartsWith("MediumToPdf/", capturedUserAgent);
    }

    [Fact]
    public async Task DownloadArticleAsync_404_ThrowsArticleNotFoundException()
    {
        var httpClient = CreateHttpClient((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound)));

        var service = new ArticleDownloadService(httpClient, delayMs: 0);

        var ex = await Assert.ThrowsAsync<ArticleNotFoundException>(
            () => service.DownloadArticleAsync("https://medium.com/not-found"));

        Assert.Equal("https://medium.com/not-found", ex.Url);
    }

    [Fact]
    public async Task DownloadArticleAsync_429_RetriesThenThrowsRateLimitExceeded()
    {
        var attemptCount = 0;
        var httpClient = CreateHttpClient((_, _) =>
        {
            attemptCount++;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.TooManyRequests));
        });

        var service = new ArticleDownloadService(httpClient, delayMs: 0);

        await Assert.ThrowsAsync<RateLimitExceededException>(
            () => service.DownloadArticleAsync("https://medium.com/article"));

        Assert.Equal(4, attemptCount); // 1 initial + 3 retries
    }

    [Fact]
    public async Task DownloadArticleAsync_500_RetriesThenThrowsArticleDownloadException()
    {
        var attemptCount = 0;
        var httpClient = CreateHttpClient((_, _) =>
        {
            attemptCount++;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError));
        });

        var service = new ArticleDownloadService(httpClient, delayMs: 0);

        var ex = await Assert.ThrowsAsync<ArticleDownloadException>(
            () => service.DownloadArticleAsync("https://medium.com/article"));

        Assert.Equal(HttpStatusCode.InternalServerError, ex.StatusCode);
        Assert.Equal(4, attemptCount); // 1 initial + 3 retries
    }

    [Fact]
    public async Task DownloadArticleAsync_RetryExponentialBackoff_VerifiesAttemptCount()
    {
        var attemptCount = 0;
        var httpClient = CreateHttpClient((_, _) =>
        {
            attemptCount++;
            if (attemptCount < 3)
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("<html>recovered</html>"),
            });
        });

        var service = new ArticleDownloadService(httpClient, delayMs: 0);

        var result = await service.DownloadArticleAsync("https://medium.com/article");

        Assert.Equal("<html>recovered</html>", result);
        Assert.Equal(3, attemptCount); // 2 failures + 1 success
    }

    [Fact]
    public async Task DownloadArticleAsync_CancellationToken_ThrowsOperationCanceled()
    {
        var httpClient = CreateHttpClient(async (_, token) =>
        {
            await Task.Delay(5000, token);
            return new HttpResponseMessage(HttpStatusCode.OK);
        });

        var service = new ArticleDownloadService(httpClient, delayMs: 0);
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => service.DownloadArticleAsync("https://medium.com/article", cts.Token));
    }

    [Fact]
    public async Task DownloadArticleAsync_EmptyUrl_ThrowsArgumentException()
    {
        var httpClient = CreateHttpClient((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)));

        var service = new ArticleDownloadService(httpClient, delayMs: 0);

        await Assert.ThrowsAsync<ArgumentException>(
            () => service.DownloadArticleAsync(""));
    }

    [Fact]
    public async Task DownloadArticleAsync_404_DoesNotRetry()
    {
        var attemptCount = 0;
        var httpClient = CreateHttpClient((_, _) =>
        {
            attemptCount++;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        });

        var service = new ArticleDownloadService(httpClient, delayMs: 0);

        await Assert.ThrowsAsync<ArticleNotFoundException>(
            () => service.DownloadArticleAsync("https://medium.com/not-found"));

        Assert.Equal(1, attemptCount); // No retries for 404
    }

    [Fact]
    public void ArticleNotFoundException_IsArticleDownloadException()
    {
        var ex = new ArticleNotFoundException("https://medium.com/test");

        Assert.IsAssignableFrom<ArticleDownloadException>(ex);
        Assert.Equal(HttpStatusCode.NotFound, ex.StatusCode);
    }

    [Fact]
    public void RateLimitExceededException_IsArticleDownloadException()
    {
        var ex = new RateLimitExceededException("https://medium.com/test");

        Assert.IsAssignableFrom<ArticleDownloadException>(ex);
        Assert.Equal(HttpStatusCode.TooManyRequests, ex.StatusCode);
    }

    [Fact]
    public void GetUserAgent_ReturnsExpectedFormat()
    {
        var userAgent = ArticleDownloadService.GetUserAgent();

        Assert.StartsWith("MediumToPdf/", userAgent);
        Assert.Matches(@"^MediumToPdf/\d+\.\d+\.\d+$", userAgent);
    }
}
