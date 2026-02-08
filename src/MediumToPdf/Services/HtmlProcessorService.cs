using AngleSharp;
using AngleSharp.Dom;
using MediumToPdf.Models;

namespace MediumToPdf.Services;

public sealed class HtmlProcessorService : IHtmlProcessorService
{
    private const string _articleH1Selector = "article h1, main h1";
    private const string _ogTitleSelector = "meta[property='og:title']";
    private const string _titleTagSelector = "title";
    private const string _authorMetaSelector = "meta[name='author']";
    private const string _publishedTimeSelector = "meta[property='article:published_time']";
    private const string _timeElementSelector = "time[datetime]";
    private const string _articleSelector = "article";
    private const string _mainSelector = "main";
    private const string _nonContentSelectors = "nav, button, footer, script, style";

    public async Task<ArticleContent> ExtractArticleAsync(
        string html,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(html);

        var config = Configuration.Default;
        using var context = BrowsingContext.New(config);
        using var document = await context.OpenAsync(
            req => req.Content(html),
            cancellationToken);

        var title = ExtractTitle(document);
        var author = ExtractAuthor(document);
        var publishDate = ExtractPublishDate(document);
        var bodyHtml = ExtractBodyHtml(document, html);

        return new ArticleContent(title, author, publishDate, bodyHtml);
    }

    private static string ExtractTitle(IDocument document)
    {
        var h1 = document.QuerySelector(_articleH1Selector);
        if (h1 is not null)
        {
            return h1.TextContent.Trim();
        }

        var ogTitle = document.QuerySelector(_ogTitleSelector);
        if (ogTitle is not null)
        {
            var content = ogTitle.GetAttribute("content");
            if (!string.IsNullOrWhiteSpace(content))
            {
                return content.Trim();
            }
        }

        var titleTag = document.QuerySelector(_titleTagSelector);
        if (titleTag is not null)
        {
            var text = titleTag.TextContent.Trim();
            if (!string.IsNullOrWhiteSpace(text))
            {
                return text;
            }
        }

        throw new HtmlProcessingException("No title found in HTML content.", document.Source.Text);
    }

    private static string? ExtractAuthor(IDocument document)
    {
        var meta = document.QuerySelector(_authorMetaSelector);
        return meta?.GetAttribute("content")?.Trim();
    }

    private static DateOnly? ExtractPublishDate(IDocument document)
    {
        var meta = document.QuerySelector(_publishedTimeSelector);
        var dateStr = meta?.GetAttribute("content");

        if (string.IsNullOrWhiteSpace(dateStr))
        {
            var timeEl = document.QuerySelector(_timeElementSelector);
            dateStr = timeEl?.GetAttribute("datetime");
        }

        if (!string.IsNullOrWhiteSpace(dateStr) && DateTime.TryParse(dateStr, out var dt))
        {
            return DateOnly.FromDateTime(dt);
        }

        return null;
    }

    private static string ExtractBodyHtml(IDocument document, string originalHtml)
    {
        var body = document.QuerySelector(_articleSelector)
                   ?? document.QuerySelector(_mainSelector);

        if (body is null)
        {
            throw new HtmlProcessingException(
                "No article or main element found in HTML content.",
                originalHtml);
        }

        var clone = body.Clone(true) as IElement;
        if (clone is null)
        {
            throw new HtmlProcessingException(
                "Failed to process body content.",
                originalHtml);
        }

        StripNonContentElements(clone);
        RemoveInlineStyles(clone);

        var cleanHtml = clone.InnerHtml.Trim();
        if (string.IsNullOrWhiteSpace(cleanHtml))
        {
            throw new HtmlProcessingException(
                "Body content is empty after extraction.",
                originalHtml);
        }

        return cleanHtml;
    }

    private static void StripNonContentElements(IElement element)
    {
        var toRemove = element.QuerySelectorAll(_nonContentSelectors);
        foreach (var el in toRemove)
        {
            el.Remove();
        }
    }

    private static void RemoveInlineStyles(IElement element)
    {
        var styled = element.QuerySelectorAll("[style]");
        foreach (var el in styled)
        {
            el.RemoveAttribute("style");
        }

        var classed = element.QuerySelectorAll("[class]");
        foreach (var el in classed)
        {
            el.RemoveAttribute("class");
        }
    }
}
