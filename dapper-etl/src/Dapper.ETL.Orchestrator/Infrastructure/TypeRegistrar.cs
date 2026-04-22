using Microsoft.Extensions.DependencyInjection;
using Spectre.Console.Cli;

namespace Dapper.ETL.Orchestrator.Infrastructure;

/// <summary>
/// Adapts Microsoft.Extensions.DependencyInjection to Spectre.Console.Cli's ITypeRegistrar.
/// </summary>
public sealed class TypeRegistrar : ITypeRegistrar {
    private readonly IServiceCollection _services;

    /// <summary>
    /// Initializes a new instance of the <see cref="TypeRegistrar" /> class.
    /// </summary>
    public TypeRegistrar(IServiceCollection services) {
        _services = services;
    }

    /// <inheritdoc />
    public ITypeResolver Build() {
        return new TypeResolver(_services.BuildServiceProvider());
    }

    /// <inheritdoc />
    public void Register(Type service, Type implementation) {
        _services.AddSingleton(service, implementation);
    }

    /// <inheritdoc />
    public void RegisterInstance(Type service, object implementation) {
        _services.AddSingleton(service, implementation);
    }

    /// <inheritdoc />
    public void RegisterLazy(Type service, Func<object> factory) {
        _services.AddSingleton(service, _ => factory());
    }
}

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