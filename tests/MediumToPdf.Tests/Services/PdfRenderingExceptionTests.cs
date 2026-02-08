using MediumToPdf.Services;
using Xunit;

namespace MediumToPdf.Tests.Services;

public sealed class PdfRenderingExceptionTests
{
    [Fact]
    public void PdfRenderingException_StoresOutputPathAndMessage()
    {
        var ex = new PdfRenderingException("Render failed", "/tmp/output.pdf");

        Assert.Equal("Render failed", ex.Message);
        Assert.Equal("/tmp/output.pdf", ex.OutputPath);
    }

    [Fact]
    public void BrowserDownloadException_InheritsFromBase()
    {
        var inner = new InvalidOperationException("Network error");
        var ex = new BrowserDownloadException("Chromium download failed", inner);

        Assert.IsAssignableFrom<PdfRenderingException>(ex);
        Assert.Equal("Chromium download failed", ex.Message);
        Assert.Null(ex.OutputPath);
        Assert.Same(inner, ex.InnerException);
    }

    [Fact]
    public void BrowserLaunchException_InheritsFromBase()
    {
        var ex = new BrowserLaunchException("Failed to start browser");

        Assert.IsAssignableFrom<PdfRenderingException>(ex);
        Assert.Equal("Failed to start browser", ex.Message);
        Assert.Null(ex.OutputPath);
    }

    [Fact]
    public void PdfGenerationFailedException_InheritsFromBaseWithOutputPath()
    {
        var ex = new PdfGenerationFailedException(
            "PDF write failed", "/tmp/output.pdf", new IOException("Disk full"));

        Assert.IsAssignableFrom<PdfRenderingException>(ex);
        Assert.Equal("PDF write failed", ex.Message);
        Assert.Equal("/tmp/output.pdf", ex.OutputPath);
        Assert.IsType<IOException>(ex.InnerException);
    }
}
