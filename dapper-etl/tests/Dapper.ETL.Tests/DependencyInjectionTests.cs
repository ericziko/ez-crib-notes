using Dapper.ETL.Library;
using Dapper.ETL.Library.Implementation;
using Dapper.ETL.Library.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Dapper.ETL.Tests;

/// <summary>
///     Tests for dependency injection configuration
/// </summary>
public class DependencyInjectionTests {
    /// <summary>
    ///     Helper to configure EtlOptions with a no-op source connection string for unit tests
    ///     that don't actually open database connections.
    /// </summary>
    private static void ConfigureTestOptions(EtlOptions opts) {
        opts.SourceConnectionString = "Server=.;Database=Test;Integrated Security=true;";
        opts.TargetConnectionString = "Server=.;Database=TestTarget;Integrated Security=true;";
        opts.LogsConnectionString = "Server=.;Database=TestLogs;Integrated Security=true;";
    }

    [Fact]
    public void AddEtlServices_RegistersAllServices() {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEtlServices(ConfigureTestOptions);
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        Assert.NotNull(serviceProvider.GetRequiredService<IEtlLogger>());
        Assert.NotNull(serviceProvider.GetRequiredService<IColumnMapper>());
        Assert.NotNull(serviceProvider.GetRequiredService<IBatchProcessor>());
        Assert.NotNull(serviceProvider.GetRequiredService<ITransactionManager>());
        Assert.NotNull(serviceProvider.GetRequiredService<ITableCopyService>());
        Assert.NotNull(serviceProvider.GetRequiredService<IStoredProcedureService>());
        Assert.NotNull(serviceProvider.GetRequiredService<IEtlOrchestrator>());
    }

    [Fact]
    public void AddEtlServices_RegistersEtlLoggerAsSingleton() {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEtlServices(ConfigureTestOptions);
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var logger1 = serviceProvider.GetRequiredService<IEtlLogger>();
        var logger2 = serviceProvider.GetRequiredService<IEtlLogger>();
        Assert.Same(logger1, logger2);
    }

    [Fact]
    public void AddEtlServices_RegistersColumnMapperAsSingleton() {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEtlServices(ConfigureTestOptions);
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var mapper1 = serviceProvider.GetRequiredService<IColumnMapper>();
        var mapper2 = serviceProvider.GetRequiredService<IColumnMapper>();
        Assert.Same(mapper1, mapper2);
    }

    [Fact]
    public void AddEtlServices_RegistersBatchProcessorAsSingleton() {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEtlServices(ConfigureTestOptions);
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var processor1 = serviceProvider.GetRequiredService<IBatchProcessor>();
        var processor2 = serviceProvider.GetRequiredService<IBatchProcessor>();
        Assert.Same(processor1, processor2);
    }

    [Fact]
    public void AddEtlServices_RegistersTransactionManagerAsTransient() {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEtlServices(ConfigureTestOptions);
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var manager1 = serviceProvider.GetRequiredService<ITransactionManager>();
        var manager2 = serviceProvider.GetRequiredService<ITransactionManager>();
        Assert.NotSame(manager1, manager2);
    }

    [Fact]
    public void AddEtlServices_RegistersTableCopyServiceAsTransient() {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEtlServices(ConfigureTestOptions);
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var service1 = serviceProvider.GetRequiredService<ITableCopyService>();
        var service2 = serviceProvider.GetRequiredService<ITableCopyService>();
        Assert.NotSame(service1, service2);
    }

    [Fact]
    public void AddEtlServices_RegistersStoredProcedureServiceAsTransient() {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEtlServices(ConfigureTestOptions);
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var service1 = serviceProvider.GetRequiredService<IStoredProcedureService>();
        var service2 = serviceProvider.GetRequiredService<IStoredProcedureService>();
        Assert.NotSame(service1, service2);
    }

    [Fact]
    public void AddEtlServices_RegistersEtlOrchestratorAsTransient() {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEtlServices(ConfigureTestOptions);
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var orchestrator1 = serviceProvider.GetRequiredService<IEtlOrchestrator>();
        var orchestrator2 = serviceProvider.GetRequiredService<IEtlOrchestrator>();
        Assert.NotSame(orchestrator1, orchestrator2);
    }

    [Fact]
    public void AddEtlServices_RegistersConcreteImplementations() {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEtlServices(ConfigureTestOptions);
        var serviceProvider = services.BuildServiceProvider();

        // Assert — IEtlLogger is now backed by SerilogEtlLogger (Phase 0.1)
        Assert.IsType<SerilogEtlLogger>(serviceProvider.GetRequiredService<IEtlLogger>());
        Assert.IsType<ColumnMapper>(serviceProvider.GetRequiredService<IColumnMapper>());
        Assert.IsType<BatchProcessor>(serviceProvider.GetRequiredService<IBatchProcessor>());
        Assert.IsType<TransactionManager>(serviceProvider.GetRequiredService<ITransactionManager>());
        Assert.IsType<TableCopyService>(serviceProvider.GetRequiredService<ITableCopyService>());
        Assert.IsType<StoredProcedureService>(serviceProvider.GetRequiredService<IStoredProcedureService>());
        Assert.IsType<EtlOrchestrator>(serviceProvider.GetRequiredService<IEtlOrchestrator>());
    }

    [Fact]
    public void AddEtlServices_ReturnsSameServiceCollection() {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var result = services.AddEtlServices(ConfigureTestOptions);

        // Assert
        Assert.Same(services, result);
    }

    [Fact]
    public void AddEtlServices_CanBeCalledMultipleTimes() {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEtlServices(ConfigureTestOptions);
        services.AddEtlServices(ConfigureTestOptions); // Call again

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        Assert.NotNull(serviceProvider.GetRequiredService<IEtlLogger>());
    }

    [Fact]
    public void AddEtlServices_InjectsTableCopyServiceWithDependencies() {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEtlServices(ConfigureTestOptions);
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var service = serviceProvider.GetRequiredService<ITableCopyService>();
        Assert.NotNull(service);
        // Verify that it was instantiated with its dependencies
        Assert.IsType<TableCopyService>(service);
    }

    [Fact]
    public void AddEtlServices_InjectsStoredProcedureServiceWithDependencies() {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEtlServices(ConfigureTestOptions);
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var service = serviceProvider.GetRequiredService<IStoredProcedureService>();
        Assert.NotNull(service);
        Assert.IsType<StoredProcedureService>(service);
    }

    [Fact]
    public void AddEtlServices_InjectsEtlOrchestratorWithDependencies() {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEtlServices(ConfigureTestOptions);
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var orchestrator = serviceProvider.GetRequiredService<IEtlOrchestrator>();
        Assert.NotNull(orchestrator);
        Assert.IsType<EtlOrchestrator>(orchestrator);
    }

    [Fact]
    public void AddEtlServices_CanResolveAllServicesMultipleTimes() {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEtlServices(ConfigureTestOptions);
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        for (var i = 0; i < 5; i++) {
            var logger = serviceProvider.GetRequiredService<IEtlLogger>();
            var mapper = serviceProvider.GetRequiredService<IColumnMapper>();
            var processor = serviceProvider.GetRequiredService<IBatchProcessor>();
            var manager = serviceProvider.GetRequiredService<ITransactionManager>();
            var tableCopy = serviceProvider.GetRequiredService<ITableCopyService>();
            var storedProc = serviceProvider.GetRequiredService<IStoredProcedureService>();
            var orchestrator = serviceProvider.GetRequiredService<IEtlOrchestrator>();

            Assert.NotNull(logger);
            Assert.NotNull(mapper);
            Assert.NotNull(processor);
            Assert.NotNull(manager);
            Assert.NotNull(tableCopy);
            Assert.NotNull(storedProc);
            Assert.NotNull(orchestrator);
        }
    }
}