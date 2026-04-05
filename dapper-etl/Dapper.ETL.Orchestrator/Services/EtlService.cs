namespace Dapper.ETL.Orchestrator.Services
{
    using Dapper.ETL.Library.Models;
    using Dapper.ETL.Orchestrator.Models;
    using Microsoft.Data.SqlClient;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// Service that coordinates ETL operations and seed data generation.
    /// </summary>
    public class EtlService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EtlService> _logger;

        private static readonly string[] FirstNames = ["John", "Jane", "Alice", "Bob", "Carol", "Dave", "Eve", "Frank", "Grace", "Hank"];
        private static readonly string[] LastNames = ["Smith", "Jones", "Taylor", "Brown", "Wilson", "Davis", "Clark", "Lewis", "Walker", "Hall"];

        /// <summary>
        /// Initializes a new instance of the <see cref="EtlService"/> class.
        /// </summary>
        public EtlService(IConfiguration configuration, ILogger<EtlService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        /// <summary>
        /// Runs the ETL pipeline with the specified transaction mode.
        /// Stub implementation — will be expanded in Phase 4.2-4.3.
        /// </summary>
        public Task<EtlResult> RunEtl(EtlTransactionMode mode)
        {
            _logger.LogInformation("RunEtl called with mode {Mode} (stub)", mode);

            return Task.FromResult(new EtlResult
            {
                Success = true,
                RowsCopied = 0,
                Duration = 0,
                ErrorMessage = null
            });
        }

        /// <summary>
        /// Seeds the source Customer table with the specified number of test rows.
        /// </summary>
        public async Task<int> SeedCustomers(int count)
        {
            var connectionString = _configuration["ConnectionStrings:Source"];
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException("ConnectionStrings:Source is not configured.");
            }

            _logger.LogInformation("Seeding {Count} customers into source database", count);

            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            const string insertSql = """
                INSERT INTO [dbo].[Customer] ([CustomerId], [FirstName], [LastName], [EmailAddress])
                VALUES (@CustomerId, @FirstName, @LastName, @EmailAddress)
                """;

            int inserted = 0;
            for (int i = 1; i <= count; i++)
            {
                var firstName = FirstNames[(i - 1) % FirstNames.Length];
                var lastName = LastNames[(i - 1) % LastNames.Length];
                var email = $"{firstName.ToLowerInvariant()}.{lastName.ToLowerInvariant()}{i}@example.com";

                await using var cmd = new SqlCommand(insertSql, connection);
                cmd.Parameters.AddWithValue("@CustomerId", i);
                cmd.Parameters.AddWithValue("@FirstName", firstName);
                cmd.Parameters.AddWithValue("@LastName", lastName);
                cmd.Parameters.AddWithValue("@EmailAddress", email);

                await cmd.ExecuteNonQueryAsync();
                inserted++;
            }

            _logger.LogInformation("Seeded {Inserted} customers successfully", inserted);
            return inserted;
        }
    }
}
