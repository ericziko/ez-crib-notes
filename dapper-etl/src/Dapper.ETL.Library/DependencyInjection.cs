namespace Dapper.ETL.Library
{
    using System;
    using Dapper.ETL.Library.Implementation;
    using Dapper.ETL.Library.Interfaces;
    using Microsoft.Data.SqlClient;
    using Microsoft.Extensions.DependencyInjection;

    /// <summary>
    /// Configuration options for ETL services, supporting multi-connection routing.
    /// </summary>
    public class EtlOptions
    {
        /// <summary>
        /// Connection string for the source database (read operations).
        /// </summary>
        public string SourceConnectionString { get; set; } = string.Empty;

        /// <summary>
        /// Connection string for the target database (write operations).
        /// Reserved for future orchestrator use.
        /// </summary>
        public string TargetConnectionString { get; set; } = string.Empty;

        /// <summary>
        /// Connection string for the logs database (audit/logging operations).
        /// Reserved for future orchestrator use.
        /// </summary>
        public string LogsConnectionString { get; set; } = string.Empty;
    }

    /// <summary>
    /// Extension methods for registering ETL services with dependency injection.
    /// </summary>
    public static class DependencyInjection
    {
        /// <summary>
        /// Adds ETL services to the service collection with multi-connection routing support.
        /// </summary>
        /// <remarks>
        /// Phase 0.1: Refactored to accept <see cref="EtlOptions"/> for source/target/logs connection routing.
        /// Target and Logs connections are registered as infrastructure but not yet wired into
        /// service registrations — that is a separate orchestrator concern.
        /// Callers must update their registration call to provide the configure delegate.
        /// </remarks>
        /// <param name="services">The service collection.</param>
        /// <param name="configure">Action to configure <see cref="EtlOptions"/>.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddEtlServices(this IServiceCollection services,
            Action<EtlOptions> configure)
        {
            var options = new EtlOptions();
            configure(options);

            // Serilog-backed logger (replaces basic console EtlLogger).
            services.AddSingleton<IEtlLogger, SerilogEtlLogger>();

            services.AddSingleton<IColumnMapper, ColumnMapper>();
            services.AddSingleton<IBatchProcessor, BatchProcessor>();

            // Source connection: used for schema inspection and read-side transaction management.
            services.AddTransient<ITransactionManager>(sp =>
                new TransactionManager(new SqlConnection(options.SourceConnectionString)));

            services.AddTransient<ISchemaInspector>(sp =>
                new SqlServerSchemaInspector(sp.GetRequiredService<ITransactionManager>().Connection));

            services.AddTransient<ITableCopyService, TableCopyService>();
            services.AddTransient<IStoredProcedureService, StoredProcedureService>();
            services.AddTransient<IEtlOrchestrator, EtlOrchestrator>();

            return services;
        }
    }
}
