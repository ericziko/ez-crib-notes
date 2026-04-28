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