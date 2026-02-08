using MediumToPdf.Services;
using PuppeteerSharp;
using Xunit;

namespace MediumToPdf.Tests.Services;

[Trait("Category", "Integration")]
public sealed class BrowserManagerTests : IAsyncLifetime
{
    private readonly BrowserManager _sut = new();
    private IBrowser? _browser;

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        if (_browser is not null)
        {
            await _browser.DisposeAsync();
        }
    }

    [Fact]
    public async Task GetBrowserAsync_ReturnsNonNullBrowser()
    {
        _browser = await _sut.GetBrowserAsync();

        Assert.NotNull(_browser);
    }

    [Fact]
    public async Task GetBrowserAsync_ReturnsSameInstanceOnSubsequentCalls()
    {
        _browser = await _sut.GetBrowserAsync();
        var second = await _sut.GetBrowserAsync();

        Assert.Same(_browser, second);
    }

    [Fact]
    public async Task GetBrowserAsync_ConcurrentCalls_ReturnSameInstance()
    {
        var tasks = Enumerable.Range(0, 5)
            .Select(_ => _sut.GetBrowserAsync())
            .ToArray();

        var browsers = await Task.WhenAll(tasks);
        _browser = browsers[0];

        Assert.All(browsers, b => Assert.Same(_browser, b));
    }
}
