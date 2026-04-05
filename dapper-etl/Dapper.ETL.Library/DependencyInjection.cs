namespace Dapper.ETL.Library
{
    using System;
    using Dapper.ETL.Library.Implementation;
    using Dapper.ETL.Library.Interfaces;
    using Microsoft.Extensions.DependencyInjection;

    /// <summary>
    /// Extension methods for registering ETL services with dependency injection.
    /// </summary>
    public static class DependencyInjection
    {
        /// <summary>
        /// Adds ETL services to the service collection.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddEtlServices(this IServiceCollection services)
        {
            services.AddSingleton<IEtlLogger, EtlLogger>();
            services.AddSingleton<IColumnMapper, ColumnMapper>();
            services.AddSingleton<IBatchProcessor, BatchProcessor>();
            services.AddTransient<ITransactionManager>(sp =>
                new TransactionManager(new Microsoft.Data.SqlClient.SqlConnection()));
            services.AddTransient<ITableCopyService, TableCopyService>();
            services.AddTransient<IStoredProcedureService, StoredProcedureService>();
            services.AddTransient<IEtlOrchestrator, EtlOrchestrator>();

            return services;
        }
    }
}
