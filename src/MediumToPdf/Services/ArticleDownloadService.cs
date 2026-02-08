using System.Net;
using System.Reflection;
using Polly;
using Polly.Retry;

namespace MediumToPdf.Services;

public sealed class ArticleDownloadService : IArticleDownloadService
{
    private const int _defaultDelayMs = 2000;
    private const int _maxRetryAttempts = 3;

    private readonly HttpClient _httpClient;
    private readonly ResiliencePipeline<HttpResponseMessage> _pipeline;
    private readonly int _delayMs;

    public ArticleDownloadService(HttpClient httpClient, int delayMs = _defaultDelayMs)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _delayMs = delayMs;
        _pipeline = BuildResiliencePipeline();
    }

    public async Task<string> DownloadArticleAsync(string url, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(url);

        await Task.Delay(_delayMs, cancellationToken);

        var response = await _pipeline.ExecuteAsync(
            async token =>
            {
                var request = new HttpRequestMessage(HttpMethod.Get, url);
                return await _httpClient.SendAsync(request, token);
            },
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            ThrowForStatusCode(url, response.StatusCode);
        }

        return await response.Content.ReadAsStringAsync(cancellationToken);
    }

    internal static string GetUserAgent()
    {
        var version = Assembly.GetExecutingAssembly().GetName().Version;
        var versionString = version != null ? $"{version.Major}.{version.Minor}.{version.Build}" : "0.0.0";
        return $"MediumToPdf/{versionString}";
    }

    private static void ThrowForStatusCode(string url, HttpStatusCode statusCode)
    {
        throw statusCode switch
        {
            HttpStatusCode.NotFound => new ArticleNotFoundException(url),
            HttpStatusCode.TooManyRequests => new RateLimitExceededException(url),
            _ => new ArticleDownloadException(url, statusCode),
        };
    }

    private static ResiliencePipeline<HttpResponseMessage> BuildResiliencePipeline()
    {
        return new ResiliencePipelineBuilder<HttpResponseMessage>()
            .AddRetry(new RetryStrategyOptions<HttpResponseMessage>
            {
                ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                    .Handle<HttpRequestException>()
                    .HandleResult(r => IsTransientStatusCode(r.StatusCode)),
                MaxRetryAttempts = _maxRetryAttempts,
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true,
                Delay = TimeSpan.FromSeconds(1),
            })
            .Build();
    }

    private static bool IsTransientStatusCode(HttpStatusCode statusCode)
    {
        return statusCode is HttpStatusCode.TooManyRequests
            or HttpStatusCode.InternalServerError
            or HttpStatusCode.BadGateway
            or HttpStatusCode.ServiceUnavailable
            or HttpStatusCode.GatewayTimeout;
    }
}
