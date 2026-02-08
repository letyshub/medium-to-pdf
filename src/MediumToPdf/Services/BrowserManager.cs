using PuppeteerSharp;
using Spectre.Console;

namespace MediumToPdf.Services;

public sealed class BrowserManager : IBrowserManager
{
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private IBrowser? _browser;

    public async Task<IBrowser> GetBrowserAsync(CancellationToken cancellationToken = default)
    {
        if (_browser is not null)
        {
            return _browser;
        }

        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            if (_browser is not null)
            {
                return _browser;
            }

            await DownloadChromiumAsync();
            _browser = await LaunchBrowserAsync();
            return _browser;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private static async Task DownloadChromiumAsync()
    {
        try
        {
            var browserFetcher = new BrowserFetcher();
            AnsiConsole.MarkupLine("[blue]Downloading Chromium...[/]");
            await browserFetcher.DownloadAsync();
            AnsiConsole.MarkupLine("[green]Chromium download complete.[/]");
        }
        catch (Exception ex) when (ex is not BrowserDownloadException)
        {
            throw new BrowserDownloadException(
                $"Failed to download Chromium: {ex.Message}", ex);
        }
    }

    private static async Task<IBrowser> LaunchBrowserAsync()
    {
        try
        {
            return await Puppeteer.LaunchAsync(new LaunchOptions
            {
                Headless = true,
            });
        }
        catch (Exception ex) when (ex is not BrowserLaunchException)
        {
            throw new BrowserLaunchException(
                $"Failed to launch browser: {ex.Message}", ex);
        }
    }
}
