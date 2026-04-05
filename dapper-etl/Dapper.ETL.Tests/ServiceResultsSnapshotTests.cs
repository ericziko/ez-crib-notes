namespace Dapper.ETL.Tests
{
    using System;
    using System.Collections.Generic;
    using Dapper.ETL.Library.Models;
    using Xunit;

    /// <summary>
    /// Tests for service result objects - comprehensive coverage of all combinations
    /// </summary>
    public class ServiceResultsSnapshotTests
    {
        [Fact]
        public void TableCopyResult_ZeroRowsCopied_Successful()
        {
            var result = new TableCopyResult(true, "SourceEmptyTable", "TargetEmptyTable", 0, 100);
            Assert.True(result.Success);
            Assert.Equal(0, result.RowCount);
            Assert.Equal(100, result.DurationMs);
        }

        [Fact]
        public void TableCopyResult_LargeRowCount_Successful()
        {
            var result = new TableCopyResult(true, "LargeSourceTable", "LargeTargetTable", 1000000, 300000);
            Assert.True(result.Success);
            Assert.Equal(1000000, result.RowCount);
            Assert.Equal(300000, result.DurationMs);
        }

        [Fact]
        public void TableCopyResult_WithDetailedErrorMessage()
        {
            var errorMsg = "Constraint violation on primary key. Timeout occurred after 30000ms. Connection failed to remote server.";
            var result = new TableCopyResult(false, "ProblematicTable", "TargetTable", 0, 30000, errorMsg);

            Assert.False(result.Success);
            Assert.Contains("Constraint violation", result.ErrorMessage!);
            Assert.Contains("Timeout", result.ErrorMessage!);
        }

        [Fact]
        public void StoredProcedureResult_MultipleRowsAffected_Successful()
        {
            var result = new StoredProcedureResult(true, "sp_BulkUpdateUserStats", 50000);
            Assert.True(result.Success);
            Assert.Equal(50000, result.RowsAffected);
        }

        [Fact]
        public void StoredProcedureResult_ZeroRowsAffected_Successful()
        {
            var result = new StoredProcedureResult(true, "sp_NoOp", 0);
            Assert.True(result.Success);
            Assert.Equal(0, result.RowsAffected);
        }

        [Fact]
        public void StoredProcedureResult_WithDetailedErrorMessage()
        {
            var result = new StoredProcedureResult(
                false,
                "sp_FailingProcedure",
                0,
                "Procedure failed: Object reference not set to an instance of an object.");

            Assert.False(result.Success);
            Assert.Contains("Object reference", result.ErrorMessage!);
        }

        [Fact]
        public void EtlExecutionResult_SingleTableMultipleProcedures()
        {
            var tableResults = new List<TableCopyResult>
            {
                new(true, "Users", "Users", 5000, 10000)
            };
            var spResults = new List<StoredProcedureResult>
            {
                new(true, "sp_UpdateUserStats", 5000),
                new(true, "sp_RebuildIndexes", 1),
                new(true, "sp_UpdateLastModified", 5000)
            };

            var result = new EtlExecutionResult(true, tableResults, spResults);
            Assert.True(result.Success);
            Assert.Single(result.TableCopyResults);
            Assert.Equal(3, result.StoredProcedureResults.Count);
        }

        [Fact]
        public void EtlExecutionResult_MultipleTables_PartialSuccess()
        {
            var tableResults = new List<TableCopyResult>
            {
                new(true, "Users", "Users", 1000, 5000),
                new(true, "Orders", "Orders", 500, 3000),
                new(false, "Products", "Products", 0, 2000, "Constraint violation")
            };
            var spResults = new List<StoredProcedureResult>
            {
                new(true, "sp_UpdateStats", 1500)
            };

            var result = new EtlExecutionResult(false, tableResults, spResults, "Failed to copy Products table");
            Assert.False(result.Success);
            Assert.Equal(3, result.TableCopyResults.Count);
            Assert.Equal("Failed to copy Products table", result.ErrorMessage);
        }

        [Fact]
        public void EtlExecutionResult_NoTablesNoStoredProcedures()
        {
            var result = new EtlExecutionResult(true, new List<TableCopyResult>(), new List<StoredProcedureResult>());
            Assert.True(result.Success);
            Assert.Empty(result.TableCopyResults);
            Assert.Empty(result.StoredProcedureResults);
        }

        [Fact]
        public void EtlExecutionResult_ComplexScenario_ManyOperations()
        {
            var tableResults = new List<TableCopyResult>
            {
                new(true, "Customers", "Customers", 10000, 20000),
                new(true, "Orders", "Orders", 50000, 80000),
                new(true, "OrderItems", "OrderItems", 200000, 150000),
                new(true, "Products", "Products", 5000, 10000),
                new(true, "ProductCategories", "ProductCategories", 100, 5000)
            };
            var spResults = new List<StoredProcedureResult>
            {
                new(true, "sp_UpdateAllStats", 265100),
                new(true, "sp_RebuildAllIndexes", 5),
                new(true, "sp_UpdateProductCounts", 5000),
                new(true, "sp_ValidateReferentialIntegrity", 265100)
            };

            var result = new EtlExecutionResult(true, tableResults, spResults);
            Assert.True(result.Success);
            Assert.Equal(5, result.TableCopyResults.Count);
            Assert.Equal(4, result.StoredProcedureResults.Count);
        }

        [Fact]
        public void TableCopyOptions_AllPropertiesSet()
        {
            var overrides = new Dictionary<string, string>
            {
                { "SourceId", "TargetId" },
                { "SourceName", "TargetName" },
                { "SourceEmail", "TargetEmailAddress" },
                { "SourcePhone", "TargetPhoneNumber" },
                { "SourceCreatedDate", "TargetCreatedOn" }
            };
            var options = new TableCopyOptions(true, 15000, overrides);

            Assert.True(options.TruncateDestination);
            Assert.Equal(15000, options.BatchSize);
            Assert.Equal(5, options.ColumnMappingOverrides.Count);
        }

        [Fact]
        public void TableCopyOptions_MinimalConfiguration()
        {
            var options = new TableCopyOptions(true, 1000);
            Assert.True(options.TruncateDestination);
            Assert.Equal(1000, options.BatchSize);
            Assert.Empty(options.ColumnMappingOverrides);
        }

        [Fact]
        public void TableCopyOptions_NoTruncate()
        {
            var options = new TableCopyOptions(false, 5000);
            Assert.False(options.TruncateDestination);
            Assert.Equal(5000, options.BatchSize);
        }

        [Fact]
        public void StoredProcedureDefinition_ComplexParameters()
        {
            var parameters = new Dictionary<string, object?>
            {
                { "@UserId", 42 },
                { "@IsActive", true },
                { "@Score", 95.5 },
                { "@LastLogin", new DateTime(2024, 1, 15, 14, 30, 0) },
                { "@Name", "John Doe" },
                { "@Description", null }
            };
            var definition = new StoredProcedureDefinition("sp_ComplexOperation", parameters);

            Assert.Equal(6, definition.Parameters.Count);
            Assert.Equal(42, definition.Parameters["@UserId"]);
            Assert.True((bool)definition.Parameters["@IsActive"]!);
        }

        [Fact]
        public void ColumnMapping_SimpleMapping()
        {
            var mapping = new ColumnMapping("SourceUserId", "TargetUserId");
            Assert.Equal("SourceUserId", mapping.SourceColumn);
            Assert.Equal("TargetUserId", mapping.DestinationColumn);
        }

        [Fact]
        public void EtlExecutionPlan_ComplexPlanWithMultipleOperations()
        {
            var tableCopies = new List<(string SourceTable, string DestinationTable, TableCopyOptions Options)>
            {
                ("dbo.Users", "dbo.Users", new TableCopyOptions(batchSize: 10000)),
                ("dbo.Orders", "dbo.Orders", new TableCopyOptions(
                    batchSize: 20000,
                    columnMappingOverrides: new Dictionary<string, string> { { "OrderID", "Id" } })),
                ("dbo.Products", "dbo.Products", new TableCopyOptions(batchSize: 5000, truncateDestination: false))
            };
            var storedProcedures = new List<StoredProcedureDefinition>
            {
                new("sp_UpdateInventory"),
                new("sp_RecalculateOrderTotals", new Dictionary<string, object?> { { "@Recalculate", true } }),
                new("sp_ValidateData")
            };

            var plan = new EtlExecutionPlan(tableCopies, storedProcedures);
            Assert.Equal(3, plan.TableCopies.Count);
            Assert.Equal(3, plan.StoredProcedures.Count);
        }

        [Fact]
        public void TableCopyResult_Properties_AreReadOnly()
        {
            var result = new TableCopyResult(true, "Source", "Dest", 100, 1000);
            Assert.Equal("Source", result.SourceTable);
            Assert.Equal("Dest", result.DestinationTable);
            Assert.Equal(100, result.RowCount);
            Assert.Equal(1000, result.DurationMs);
            Assert.True(result.Success);
            Assert.Null(result.ErrorMessage);
        }

        [Fact]
        public void StoredProcedureResult_Properties_AreReadOnly()
        {
            var result = new StoredProcedureResult(true, "sp_Test", 50, "No error");
            Assert.Equal("sp_Test", result.ProcedureName);
            Assert.Equal(50, result.RowsAffected);
            Assert.True(result.Success);
            Assert.Equal("No error", result.ErrorMessage);
        }

        [Fact]
        public void ColumnMapping_Properties_PreserveCase()
        {
            var mapping = new ColumnMapping("SourceColumn", "TargetColumn");
            Assert.Equal("SourceColumn", mapping.SourceColumn);
            Assert.Equal("TargetColumn", mapping.DestinationColumn);
        }
    }
}
