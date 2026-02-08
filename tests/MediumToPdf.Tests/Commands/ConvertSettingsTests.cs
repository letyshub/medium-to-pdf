using MediumToPdf.Commands;
using Xunit;

namespace MediumToPdf.Tests.Commands;

public sealed class ConvertSettingsTests
{
    [Fact]
    public void Validate_EmptyUrl_ReturnsError()
    {
        var settings = new ConvertSettings
        {
            Url = "",
            Output = "output.pdf"
        };

        var result = settings.Validate();

        Assert.False(result.Successful);
    }

    [Fact]
    public void Validate_EmptyOutput_ReturnsError()
    {
        var settings = new ConvertSettings
        {
            Url = "https://medium.com/article",
            Output = ""
        };

        var result = settings.Validate();

        Assert.False(result.Successful);
    }

    [Fact]
    public void NoImages_DefaultsToFalse()
    {
        var settings = new ConvertSettings();

        Assert.False(settings.NoImages);
    }

    [Fact]
    public void Style_DefaultsToNull()
    {
        var settings = new ConvertSettings();

        Assert.Null(settings.Style);
    }
}
