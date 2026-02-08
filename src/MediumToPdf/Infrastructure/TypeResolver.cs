using Spectre.Console.Cli;

namespace MediumToPdf.Infrastructure;

public sealed class TypeResolver : ITypeResolver
{
    private readonly IServiceProvider _provider;

    public TypeResolver(IServiceProvider provider)
    {
        _provider = provider ?? throw new ArgumentNullException(nameof(provider));
    }

    public object? Resolve(Type? type)
    {
        return type != null ? _provider.GetService(type) : null;
    }
}
