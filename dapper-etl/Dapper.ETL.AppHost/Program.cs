using Aspire.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

// SQL Server container with three databases
var sqlPassword = builder.AddParameter("sql-password", "TestPassword123!", secret: true);
var sqlServer = builder.AddSqlServer("sql-server", password: sqlPassword);
sqlServer.AddDatabase("TestDbSource");
sqlServer.AddDatabase("TestDbTarget");
sqlServer.AddDatabase("EtlLogs");

// Seq container for structured logging
var seq = builder.AddContainer("seq", "datalust/seq:latest")
    .WithHttpEndpoint(port: 5341, targetPort: 5341)
    .WithEnvironment("ACCEPT_EULA", "Y");

// ETL Orchestrator project with references and environment variables
builder.AddProject<Projects.Dapper_ETL_Orchestrator>("etl-orchestrator")
    .WithReference(sqlServer)
    .WithEnvironment("ConnectionStrings__Source", "Server=localhost,1433;Database=TestDbSource;User Id=sa;Password=TestPassword123!;Encrypt=false;")
    .WithEnvironment("ConnectionStrings__Target", "Server=localhost,1433;Database=TestDbTarget;User Id=sa;Password=TestPassword123!;Encrypt=false;")
    .WithEnvironment("ConnectionStrings__Logs", "Server=localhost,1433;Database=EtlLogs;User Id=sa;Password=TestPassword123!;Encrypt=false;")
    .WithEnvironment("Seq__Url", "http://localhost:5341");

await builder.Build().RunAsync();
