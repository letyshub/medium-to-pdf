namespace MediumToPdf.Models;

public sealed record ArticleContent(
    string Title,
    string? Author,
    DateOnly? PublishDate,
    string BodyHtml);
