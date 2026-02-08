using MediumToPdf.Services;
using Xunit;

namespace MediumToPdf.Tests.Services;

public sealed class HtmlProcessorServiceTests
{
    private readonly HtmlProcessorService _service = new();

    private static string LoadFixture(string filename)
    {
        var path = Path.Combine("Fixtures", filename);
        return File.ReadAllText(path);
    }

    [Fact]
    public async Task ExtractArticleAsync_ValidHtml_ExtractsAllMetadata()
    {
        var html = LoadFixture("SampleMediumArticle.html");

        var result = await _service.ExtractArticleAsync(html);

        Assert.Equal("Understanding Async/Await in C#", result.Title);
        Assert.Equal("Jane Developer", result.Author);
        Assert.Equal(new DateOnly(2026, 1, 15), result.PublishDate);
        Assert.NotEmpty(result.BodyHtml);
    }

    [Fact]
    public async Task ExtractArticleAsync_TitleFallback_UsesOgTitleWhenNoH1()
    {
        var html = """
            <html>
            <head><meta property="og:title" content="OG Title" /></head>
            <body><article><p>Content here</p></article></body>
            </html>
            """;

        var result = await _service.ExtractArticleAsync(html);

        Assert.Equal("OG Title", result.Title);
    }

    [Fact]
    public async Task ExtractArticleAsync_AuthorFromMeta_ReturnsAuthor()
    {
        var html = """
            <html>
            <head><meta name="author" content="John Writer" /></head>
            <body><article><h1>Title</h1><p>Content</p></article></body>
            </html>
            """;

        var result = await _service.ExtractArticleAsync(html);

        Assert.Equal("John Writer", result.Author);
    }

    [Fact]
    public async Task ExtractArticleAsync_NoAuthorMeta_ReturnsNull()
    {
        var html = LoadFixture("SampleMediumArticleMinimal.html");

        var result = await _service.ExtractArticleAsync(html);

        Assert.Null(result.Author);
    }

    [Fact]
    public async Task ExtractArticleAsync_NoTimeElement_ReturnsNullDate()
    {
        var html = LoadFixture("SampleMediumArticleMinimal.html");

        var result = await _service.ExtractArticleAsync(html);

        Assert.Null(result.PublishDate);
    }

    [Fact]
    public async Task ExtractArticleAsync_StripsNonContentElements()
    {
        var html = LoadFixture("SampleMediumArticle.html");

        var result = await _service.ExtractArticleAsync(html);

        Assert.DoesNotContain("<nav", result.BodyHtml);
        Assert.DoesNotContain("<button", result.BodyHtml);
        Assert.DoesNotContain("<footer", result.BodyHtml);
        Assert.DoesNotContain("<script", result.BodyHtml);
        Assert.DoesNotContain("<style", result.BodyHtml);
    }

    [Fact]
    public async Task ExtractArticleAsync_NoTitle_ThrowsHtmlProcessingException()
    {
        var html = """
            <html><body><article><p>No title here</p></article></body></html>
            """;

        await Assert.ThrowsAsync<HtmlProcessingException>(
            () => _service.ExtractArticleAsync(html));
    }

    [Fact]
    public async Task ExtractArticleAsync_EmptyBodyAfterStripping_ThrowsHtmlProcessingException()
    {
        var html = """
            <html>
            <head><meta property="og:title" content="Title" /></head>
            <body><article><nav>Menu</nav><footer>Foot</footer><script>x</script></article></body>
            </html>
            """;

        await Assert.ThrowsAsync<HtmlProcessingException>(
            () => _service.ExtractArticleAsync(html));
    }

    [Fact]
    public async Task ExtractArticleAsync_EmptyString_ThrowsArgumentException()
    {
        await Assert.ThrowsAsync<ArgumentException>(
            () => _service.ExtractArticleAsync(""));
    }

    [Fact]
    public async Task ExtractArticleAsync_NoArticle_FallsBackToMain()
    {
        var html = """
            <html><body><main><h1>Main Title</h1><p>Main content</p></main></body></html>
            """;

        var result = await _service.ExtractArticleAsync(html);

        Assert.Equal("Main Title", result.Title);
        Assert.Contains("<p>Main content</p>", result.BodyHtml);
    }

    [Fact]
    public async Task ExtractArticleAsync_PreservesCodeBlocks()
    {
        var html = LoadFixture("SampleMediumArticle.html");

        var result = await _service.ExtractArticleAsync(html);

        Assert.Contains("<pre>", result.BodyHtml);
        Assert.Contains("<code>", result.BodyHtml);
    }

    [Fact]
    public async Task ExtractArticleAsync_PreservesImages()
    {
        var html = LoadFixture("SampleMediumArticle.html");

        var result = await _service.ExtractArticleAsync(html);

        Assert.Contains("<figure>", result.BodyHtml);
        Assert.Contains("<img", result.BodyHtml);
    }

    [Fact]
    public async Task ExtractArticleAsync_RemovesScriptAndStyleTags()
    {
        var html = """
            <html><body><article>
            <h1>Title</h1>
            <p>Content</p>
            <script>alert('x')</script>
            <style>.foo{color:red}</style>
            </article></body></html>
            """;

        var result = await _service.ExtractArticleAsync(html);

        Assert.DoesNotContain("<script", result.BodyHtml);
        Assert.DoesNotContain("<style", result.BodyHtml);
        Assert.Contains("<p>Content</p>", result.BodyHtml);
    }

    [Fact]
    public async Task ExtractArticleAsync_RemovesInlineStyles()
    {
        var html = """
            <html><body><article>
            <h1>Title</h1>
            <p style="color:red">Styled content</p>
            </article></body></html>
            """;

        var result = await _service.ExtractArticleAsync(html);

        Assert.DoesNotContain("style=", result.BodyHtml);
        Assert.Contains("Styled content", result.BodyHtml);
    }
}
