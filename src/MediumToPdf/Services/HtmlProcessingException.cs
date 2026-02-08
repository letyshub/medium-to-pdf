namespace MediumToPdf.Services;

public class HtmlProcessingException : Exception
{
    private const int _maxHtmlLength = 500;

    public string Html { get; }

    public HtmlProcessingException(string message, string html, Exception? innerException = null)
        : base(message, innerException)
    {
        Html = html.Length > _maxHtmlLength ? html[.._maxHtmlLength] : html;
    }
}
