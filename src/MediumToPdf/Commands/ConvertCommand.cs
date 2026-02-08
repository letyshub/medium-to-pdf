using MediumToPdf.Services;
using Spectre.Console;
using Spectre.Console.Cli;

namespace MediumToPdf.Commands;

public sealed class ConvertCommand : AsyncCommand<ConvertSettings>
{
    private readonly IArticleDownloadService _downloadService;
    private readonly IHtmlProcessorService _htmlProcessor;
    private readonly IPdfRenderingService _pdfRenderer;

    public ConvertCommand(
        IArticleDownloadService downloadService,
        IHtmlProcessorService htmlProcessor,
        IPdfRenderingService pdfRenderer)
    {
        _downloadService = downloadService ?? throw new ArgumentNullException(nameof(downloadService));
        _htmlProcessor = htmlProcessor ?? throw new ArgumentNullException(nameof(htmlProcessor));
        _pdfRenderer = pdfRenderer ?? throw new ArgumentNullException(nameof(pdfRenderer));
    }

    public override async Task<int> ExecuteAsync(CommandContext context, ConvertSettings settings)
    {
        AnsiConsole.MarkupLine(
            "[dim]Note: Medium's Terms of Service may prohibit automated access. " +
            "Use this tool responsibly and at your own risk.[/]");

        AnsiConsole.MarkupLine($"[blue]Downloading article:[/] {settings.Url}");

        try
        {
            var html = await _downloadService.DownloadArticleAsync(settings.Url);
            AnsiConsole.MarkupLine($"[green]Downloaded {html.Length} characters.[/]");

            var article = await _htmlProcessor.ExtractArticleAsync(html);
            AnsiConsole.MarkupLine($"[green]Title:[/] {article.Title}");
            if (article.Author is not null)
            {
                AnsiConsole.MarkupLine($"[green]Author:[/] {article.Author}");
            }

            if (article.PublishDate is not null)
            {
                AnsiConsole.MarkupLine($"[green]Date:[/] {article.PublishDate}");
            }

            AnsiConsole.MarkupLine($"[green]Body:[/] {article.BodyHtml.Length} characters");

            await _pdfRenderer.RenderPdfAsync(article, settings.Output, settings.Style);
            AnsiConsole.MarkupLine($"[green]PDF saved to:[/] {settings.Output}");
            return 0;
        }
        catch (ArticleNotFoundException ex)
        {
            AnsiConsole.MarkupLine($"[red]Article not found:[/] {ex.Url}");
            return 1;
        }
        catch (RateLimitExceededException)
        {
            AnsiConsole.MarkupLine("[red]Rate limit exceeded. Please try again later.[/]");
            return 1;
        }
        catch (ArticleDownloadException ex)
        {
            AnsiConsole.MarkupLine($"[red]Download failed:[/] {ex.Message}");
            return 1;
        }
        catch (HtmlProcessingException ex)
        {
            AnsiConsole.MarkupLine($"[red]Content extraction failed:[/] {ex.Message}");
            return 1;
        }
        catch (BrowserDownloadException ex)
        {
            AnsiConsole.MarkupLine($"[red]Browser download failed:[/] {ex.Message}");
            return 1;
        }
        catch (BrowserLaunchException ex)
        {
            AnsiConsole.MarkupLine($"[red]Browser launch failed:[/] {ex.Message}");
            return 1;
        }
        catch (PdfGenerationFailedException ex)
        {
            AnsiConsole.MarkupLine($"[red]PDF generation failed:[/] {ex.Message}");
            return 1;
        }
        catch (PdfRenderingException ex)
        {
            AnsiConsole.MarkupLine($"[red]PDF rendering error:[/] {ex.Message}");
            return 1;
        }
    }
}
