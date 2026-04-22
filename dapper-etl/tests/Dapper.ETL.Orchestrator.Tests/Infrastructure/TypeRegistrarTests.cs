using Dapper.ETL.Orchestrator.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Dapper.ETL.Orchestrator.Tests.Infrastructure;

public class TypeRegistrarTests {

    [Fact]
    public void Test_Build_ReturnsNonNullTypeResolver() {
        var registrar = new TypeRegistrar(new ServiceCollection());
        var resolver = registrar.Build();
        Assert.NotNull(resolver);
        (resolver as IDisposable)?.Dispose();
    }

    [Fact]
    public void Test_Register_ServiceCanBeResolved() {
        var services = new ServiceCollection();
        var registrar = new TypeRegistrar(services);
        registrar.Register(typeof(ITestService), typeof(TestService));
        using var resolver = (TypeResolver)registrar.Build();
        var svc = resolver.Resolve(typeof(ITestService));
        Assert.IsType<TestService>(svc);
    }

    [Fact]
    public void Test_RegisterInstance_SameInstanceReturned() {
        var services = new ServiceCollection();
        var registrar = new TypeRegistrar(services);
        var instance = new TestService();
        registrar.RegisterInstance(typeof(ITestService), instance);
        using var resolver = (TypeResolver)registrar.Build();
        var resolved = resolver.Resolve(typeof(ITestService));
        Assert.Same(instance, resolved);
    }

    [Fact]
    public void Test_RegisterLazy_FactoryInvoked() {
        var services = new ServiceCollection();
        var registrar = new TypeRegistrar(services);
        var created = new TestService();
        registrar.RegisterLazy(typeof(ITestService), () => created);
        using var resolver = (TypeResolver)registrar.Build();
        var resolved = resolver.Resolve(typeof(ITestService));
        Assert.Same(created, resolved);
    }

    [Fact]
    public void Test_TypeResolver_Resolve_NullType_ReturnsNull() {
        var provider = new ServiceCollection().BuildServiceProvider();
        using var resolver = new TypeResolver(provider);
        var result = resolver.Resolve(null);
        Assert.Null(result);
    }

    [Fact]
    public void Test_TypeResolver_Dispose_DisposesServiceProvider() {
        var services = new ServiceCollection();
        services.AddSingleton<DisposableService>();
        var provider = services.BuildServiceProvider();
        var disposable = provider.GetRequiredService<DisposableService>();
        var resolver = new TypeResolver(provider);
        resolver.Dispose();
        Assert.True(disposable.Disposed);
    }

    private interface ITestService {
        string Value { get; }
    }

    private class TestService : ITestService {
        public string Value => "test";
    }

    private class DisposableService : IDisposable {
        public bool Disposed { get; private set; }
        public void Dispose() {
            Disposed = true;
        }
    }
}