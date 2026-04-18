namespace Dapper.ETL.Orchestrator.Infrastructure;

public sealed record SqlConnectionDescriptor(
    string ServiceKey,
    string ConnectionStringKey,
    string CredentialKey);
