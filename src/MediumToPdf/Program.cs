using MediumToPdf.Commands;
using MediumToPdf.Infrastructure;
using MediumToPdf.Services;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console.Cli;

var services = new ServiceCollection();

var httpClient = new HttpClient();
httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(ArticleDownloadService.GetUserAgent());
services.AddSingleton(httpClient);
services.AddSingleton<IArticleDownloadService, ArticleDownloadService>();
services.AddSingleton<IHtmlProcessorService, HtmlProcessorService>();
services.AddSingleton<IBrowserManager, BrowserManager>();
services.AddSingleton<IPdfRenderingService, PdfRenderingService>();

var registrar = new TypeRegistrar(services);

var app = new CommandApp<ConvertCommand>(registrar);
app.Configure(config =>
{
    config.SetApplicationName("medium-to-pdf");
    config.SetApplicationVersion("1.0.0");
});
return await app.RunAsync(args);
