using System.Globalization;
using MediumToPdf.Models;
using PuppeteerSharp;
using PuppeteerSharp.Media;

namespace MediumToPdf.Services;

public sealed class PdfRenderingService : IPdfRenderingService
{
    private const string _defaultCss = """
        body {
            font-family: Georgia, 'Times New Roman', serif;
            line-height: 1.8;
            color: #333;
            max-width: 100%;
        }
        h1 {
            font-size: 2em;
            margin-bottom: 0.3em;
            line-height: 1.2;
        }
        h2, h3, h4 {
            margin-top: 1.5em;
            margin-bottom: 0.5em;
        }
        .article-meta {
            color: #666;
            font-size: 0.9em;
            margin-bottom: 2em;
            border-bottom: 1px solid #eee;
            padding-bottom: 1em;
        }
        p {
            margin-bottom: 1.2em;
        }
        img {
            max-width: 100%;
            height: auto;
            display: block;
            margin: 1.5em auto;
        }
        pre, code {
            font-family: 'Courier New', Courier, monospace;
            font-size: 0.9em;
        }
        pre {
            background-color: #f5f5f5;
            padding: 1em;
            overflow-x: auto;
            border-radius: 4px;
            line-height: 1.4;
        }
        code {
            background-color: #f5f5f5;
            padding: 0.2em 0.4em;
            border-radius: 3px;
        }
        pre code {
            background: none;
            padding: 0;
        }
        blockquote {
            border-left: 3px solid #ccc;
            padding-left: 1em;
            margin-left: 0;
            color: #555;
            font-style: italic;
        }
        a {
            color: #1a8917;
            text-decoration: none;
        }
        figure {
            margin: 1.5em 0;
        }
        figcaption {
            text-align: center;
            font-size: 0.85em;
            color: #666;
            margin-top: 0.5em;
        }
        """;

    private readonly IBrowserManager _browserManager;

    public PdfRenderingService(IBrowserManager browserManager)
    {
        _browserManager = browserManager ?? throw new ArgumentNullException(nameof(browserManager));
    }

    public async Task RenderPdfAsync(
        ArticleContent article,
        string outputPath,
        string? customCssPath,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(article);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);

        var customCss = await LoadCustomCssAsync(customCssPath, cancellationToken);
        var html = BuildHtmlDocument(article, customCss);

        try
        {
            var browser = await _browserManager.GetBrowserAsync(cancellationToken);
            await using var page = await browser.NewPageAsync();
            await page.SetContentAsync(html, new NavigationOptions
            {
                WaitUntil = [WaitUntilNavigation.Load],
            });

            await page.PdfAsync(outputPath, new PdfOptions
            {
                Format = PaperFormat.A4,
                PrintBackground = true,
                MarginOptions = new MarginOptions
                {
                    Top = "20mm",
                    Right = "15mm",
                    Bottom = "20mm",
                    Left = "15mm",
                },
            });
        }
        catch (Exception ex) when (ex is not PdfRenderingException)
        {
            throw new PdfGenerationFailedException(
                $"Failed to generate PDF: {ex.Message}", outputPath, ex);
        }
    }

    private static string BuildHtmlDocument(ArticleContent article, string? customCss)
    {
        var metaHtml = BuildMetaHtml(article);
        var customStyleTag = customCss is not null
            ? $"<style>{customCss}</style>"
            : string.Empty;

        return $"""
            <!DOCTYPE html>
            <html lang="en">
            <head>
                <meta charset="utf-8" />
                <title>{EscapeHtml(article.Title)}</title>
                <style>{_defaultCss}</style>
                {customStyleTag}
            </head>
            <body>
                {metaHtml}
                {article.BodyHtml}
            </body>
            </html>
            """;
    }

    private static string BuildMetaHtml(ArticleContent article)
    {
        var parts = new List<string>
        {
            $"<h1>{EscapeHtml(article.Title)}</h1>",
        };

        var metaLines = new List<string>();
        if (article.Author is not null)
        {
            metaLines.Add($"By {EscapeHtml(article.Author)}");
        }

        if (article.PublishDate is not null)
        {
            metaLines.Add(article.PublishDate.Value.ToString("MMMM d, yyyy", CultureInfo.InvariantCulture));
        }

        if (metaLines.Count > 0)
        {
            parts.Add($"""<div class="article-meta">{string.Join(" &middot; ", metaLines)}</div>""");
        }

        return string.Join("\n", parts);
    }

    private static string EscapeHtml(string text)
    {
        return text
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;")
            .Replace("\"", "&quot;");
    }

    private static async Task<string?> LoadCustomCssAsync(
        string? customCssPath,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(customCssPath))
        {
            return null;
        }

        if (!File.Exists(customCssPath))
        {
            throw new PdfGenerationFailedException(
                $"Custom CSS file not found: '{customCssPath}'");
        }

        return await File.ReadAllTextAsync(customCssPath, cancellationToken);
    }
}
