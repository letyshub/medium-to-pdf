using PuppeteerSharp;

namespace MediumToPdf.Services;

public interface IBrowserManager
{
    Task<IBrowser> GetBrowserAsync(CancellationToken cancellationToken = default);
}
