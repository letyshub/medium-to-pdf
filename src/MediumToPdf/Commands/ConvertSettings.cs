using System.ComponentModel;
using Spectre.Console;
using Spectre.Console.Cli;

namespace MediumToPdf.Commands;

public sealed class ConvertSettings : CommandSettings
{
    [CommandArgument(0, "<url>")]
    [Description("URL of the Medium article")]
    public string Url { get; init; } = string.Empty;

    [CommandOption("-o|--output")]
    [Description("Output PDF file path")]
    public string Output { get; init; } = string.Empty;

    [CommandOption("--no-images")]
    [Description("Skip image download")]
    [DefaultValue(false)]
    public bool NoImages { get; init; }

    [CommandOption("--style")]
    [Description("Custom CSS file for styling")]
    public string? Style { get; init; }

    public override ValidationResult Validate()
    {
        if (string.IsNullOrWhiteSpace(Url))
        {
            return ValidationResult.Error("URL is required.");
        }

        if (string.IsNullOrWhiteSpace(Output))
        {
            return ValidationResult.Error("Output file path is required.");
        }

        return ValidationResult.Success();
    }
}
