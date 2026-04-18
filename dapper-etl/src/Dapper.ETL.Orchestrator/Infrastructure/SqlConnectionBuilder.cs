namespace Dapper.ETL.Orchestrator.Infrastructure;

public sealed class SqlConnectionBuilder
{
    public List<SqlConnectionDescriptor> Descriptors { get; } = new();

    public SqlConnectionBuilder Add(string serviceKey, string connectionStringKey, string credentialKey)
    {
        Descriptors.Add(new SqlConnectionDescriptor(serviceKey, connectionStringKey, credentialKey));
        return this;
    }
}
