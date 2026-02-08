namespace MediumToPdf.Services;

public class PdfRenderingException : Exception
{
    public string? OutputPath { get; }

    public PdfRenderingException(string message, string? outputPath = null, Exception? innerException = null)
        : base(message, innerException)
    {
        OutputPath = outputPath;
    }
}

public sealed class BrowserDownloadException : PdfRenderingException
{
    public BrowserDownloadException(string message, Exception? innerException = null)
        : base(message, outputPath: null, innerException)
    {
    }
}

public sealed class BrowserLaunchException : PdfRenderingException
{
    public BrowserLaunchException(string message, Exception? innerException = null)
        : base(message, outputPath: null, innerException)
    {
    }
}

public sealed class PdfGenerationFailedException : PdfRenderingException
{
    public PdfGenerationFailedException(string message, string? outputPath = null, Exception? innerException = null)
        : base(message, outputPath, innerException)
    {
    }
}
