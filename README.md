# medium-to-pdf

![CI](https://github.com/letyshub/medium-to-pdf/actions/workflows/ci.yml/badge.svg)

A .NET 8 global tool that converts Medium articles to well-formatted PDF files.

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

Chromium is downloaded automatically on first run.

## Installation

```bash
dotnet tool install --global MediumToPdf
```

## Usage

```bash
medium-to-pdf <url> -o <output.pdf> [--style <custom.css>]
```

### Examples

```bash
# Convert an article to PDF
medium-to-pdf https://medium.com/@user/my-article -o article.pdf

# Use a custom CSS stylesheet
medium-to-pdf https://medium.com/@user/my-article -o article.pdf --style custom.css
```

### Options

| Option | Description |
|--------|-------------|
| `<url>` | URL of the Medium article (required) |
| `-o, --output` | Output PDF file path (required) |
| `--style` | Custom CSS file for styling (optional) |

## How It Works

1. Downloads the Medium article HTML
2. Extracts the article content (title, author, date, body)
3. Renders a styled PDF using headless Chromium via PuppeteerSharp

The output PDF uses A4 paper with clean typography optimized for reading. A default stylesheet provides serif fonts, properly formatted code blocks, images, and blockquotes. You can override styles with the `--style` option.

## Development

```bash
# Build
dotnet build

# Run tests
dotnet test

# Run the tool locally
dotnet run --project src/MediumToPdf -- <url> -o output.pdf
```

## License

This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for details.
