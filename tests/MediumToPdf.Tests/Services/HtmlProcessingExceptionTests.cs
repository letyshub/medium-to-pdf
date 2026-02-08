using MediumToPdf.Services;
using Xunit;

namespace MediumToPdf.Tests.Services;

public sealed class HtmlProcessingExceptionTests
{
    [Fact]
    public void Constructor_TruncatesHtmlProperty()
    {
        var longHtml = new string('x', 1000);

        var ex = new HtmlProcessingException("Error occurred", longHtml);

        Assert.True(ex.Html.Length <= 500);
    }

    [Fact]
    public void Constructor_FormatsMessage()
    {
        var ex = new HtmlProcessingException("Title not found", "<html></html>");

        Assert.Equal("Title not found", ex.Message);
        Assert.Equal("<html></html>", ex.Html);
    }
}
