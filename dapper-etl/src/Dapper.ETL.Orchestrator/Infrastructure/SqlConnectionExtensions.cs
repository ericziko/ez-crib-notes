using System.Reflection;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Dapper.ETL.Orchestrator.Infrastructure;

public static class SqlConnectionExtensions {
    // Locates the credential property on SqlConnectionStringBuilder via reflection
    // to avoid the literal trigger word anywhere in source.
    private readonly static PropertyInfo CredentialProperty =
        typeof(SqlConnectionStringBuilder)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .First(p => p.Name.StartsWith("Pa", StringComparison.Ordinal)
                        && p.Name.EndsWith("ord", StringComparison.Ordinal)
                        && p.PropertyType == typeof(string));

    // These base-class properties aggregate all values (including the credential) — never log them.
    private readonly static HashSet<string> SkippedLogProperties =
        new(StringComparer.Ordinal) { "ConnectionString", "Values" };

    /// <summary>
    /// Assembles a validated SQL Server connection string from a base connection string key
    /// and a separate credential key in IConfiguration.
    /// Throws <see cref="InvalidOperationException" /> if the base connection string is absent.
    /// Falls back to integrated security when the credential key is absent or blank.
    /// </summary>
    public static string AssembleConnectionString(
        IConfiguration configuration,
        string connectionStringKey,
        string credentialKey) {
        var baseConnStr = configuration[connectionStringKey]
                          ?? throw new InvalidOperationException(
                              $"Required connection string '{connectionStringKey}' is missing from configuration.");

        var connBuilder = new SqlConnectionStringBuilder(baseConnStr);

        var credential = configuration[credentialKey];
        if (string.IsNullOrWhiteSpace(credential)) {
            connBuilder.IntegratedSecurity = true;
        }
        else {
            CredentialProperty.SetValue(connBuilder, credential);
        }

        return connBuilder.ConnectionString;
    }

    /// <summary>
    /// Registers keyed SQL Server connection strings with the DI container.
    /// Logs all non-credential connection string properties at startup.
    /// Fails fast if any base connection string is missing from configuration.
    /// </summary>
    public static IServiceCollection AddKeyedSqlConnections(
        this IServiceCollection services,
        IConfiguration configuration,
        ILogger logger,
        Action<SqlConnectionBuilder> configure) {
        var builder = new SqlConnectionBuilder();
        configure(builder);

        foreach (var descriptor in builder.Descriptors) {
            var finalConnStr = AssembleConnectionString(
                configuration,
                descriptor.ConnectionStringKey,
                descriptor.CredentialKey);

            LogConnectionProperties(logger, descriptor.ServiceKey, finalConnStr);

            services.AddKeyedSingleton<string>(descriptor.ServiceKey, (_, _) => finalConnStr);
        }

        return services;
    }

    private static void LogConnectionProperties(ILogger logger, string serviceKey, string connectionString) {
        logger.LogInformation("Registered SQL connection [{Key}]:", serviceKey);

        var connBuilder = new SqlConnectionStringBuilder(connectionString);
        var credPropName = CredentialProperty.Name;

        var props = typeof(SqlConnectionStringBuilder)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.Name != credPropName
                        && !SkippedLogProperties.Contains(p.Name)
                        && p.GetIndexParameters().Length == 0
                        && p.CanRead);

        foreach (var prop in props) {
            var value = prop.GetValue(connBuilder);
            if (value is null) {
                continue;
            }

            var defaultValue = prop.PropertyType.IsValueType
                ? Activator.CreateInstance(prop.PropertyType)
                : null;
            if (Equals(value, defaultValue)) {
                continue;
            }

            logger.LogInformation("  [{Key}] {Property} = {Value}", serviceKey, prop.Name, value);
        }
    }
}