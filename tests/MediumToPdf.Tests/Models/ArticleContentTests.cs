using MediumToPdf.Models;
using Xunit;

namespace MediumToPdf.Tests.Models;

public sealed class ArticleContentTests
{
    [Fact]
    public void Constructor_WithAllProperties_SetsValues()
    {
        var content = new ArticleContent(
            Title: "Test Title",
            Author: "Test Author",
            PublishDate: new DateOnly(2026, 1, 15),
            BodyHtml: "<p>Body</p>");

        Assert.Equal("Test Title", content.Title);
        Assert.Equal("Test Author", content.Author);
        Assert.Equal(new DateOnly(2026, 1, 15), content.PublishDate);
        Assert.Equal("<p>Body</p>", content.BodyHtml);
    }

    [Fact]
    public void Constructor_WithNullableProperties_AllowsNull()
    {
        var content = new ArticleContent(
            Title: "Title Only",
            Author: null,
            PublishDate: null,
            BodyHtml: "<p>Content</p>");

        Assert.Equal("Title Only", content.Title);
        Assert.Null(content.Author);
        Assert.Null(content.PublishDate);
    }
}
