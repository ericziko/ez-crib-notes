using Spectre.Console.Cli;

namespace Dapper.ETL.Orchestrator.Infrastructure;

/// <summary>
/// Resolves types from a built <see cref="IServiceProvider" />.
/// </summary>
public sealed class TypeResolver : ITypeResolver, IDisposable {
    private readonly IServiceProvider _provider;

    /// <summary>
    /// Initializes a new instance of the <see cref="TypeResolver" /> class.
    /// </summary>
    public TypeResolver(IServiceProvider provider) {
        _provider = provider;
    }

    /// <inheritdoc />
    public void Dispose() {
        if (_provider is IDisposable disposable) {
            disposable.Dispose();
        }
    }

    /// <inheritdoc />
    public object? Resolve(Type? type) {
        if (type == null) {
            return null;
        }
        return _provider.GetService(type);
    }
}