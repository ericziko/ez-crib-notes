namespace Dapper.ETL.Tests
{
    using System;
    using System.Collections.Generic;
    using Dapper.ETL.Library.Models;
    using Xunit;

    /// <summary>
    /// Tests for all model classes
    /// </summary>
    public class ModelsTests
    {
        [Fact]
        public void ColumnMapping_WithValidInputs_CreatesCorrectly()
        {
            var mapping = new ColumnMapping("UserId", "Id");
            Assert.Equal("UserId", mapping.SourceColumn);
            Assert.Equal("Id", mapping.DestinationColumn);
        }

        [Fact]
        public void ColumnMapping_Constructor_ThrowsOnNullSourceColumn()
        {
            Assert.Throws<ArgumentNullException>(() => new ColumnMapping(null!, "DestCol"));
        }

        [Fact]
        public void ColumnMapping_Constructor_ThrowsOnNullDestinationColumn()
        {
            Assert.Throws<ArgumentNullException>(() => new ColumnMapping("SourceCol", null!));
        }

        [Fact]
        public void TableCopyResult_SuccessfulCopy_CreatesCorrectly()
        {
            var result = new TableCopyResult(true, "SourceUsers", "TargetUsers", 1000, 5432, null);
            Assert.True(result.Success);
            Assert.Equal("SourceUsers", result.SourceTable);
            Assert.Equal("TargetUsers", result.DestinationTable);
            Assert.Equal(1000, result.RowCount);
            Assert.Equal(5432, result.DurationMs);
            Assert.Null(result.ErrorMessage);
        }

        [Fact]
        public void TableCopyResult_FailedCopy_CreatesWithError()
        {
            var result = new TableCopyResult(false, "SourceUsers", "TargetUsers", 500, 2000, "Connection timeout");
            Assert.False(result.Success);
            Assert.Equal("Connection timeout", result.ErrorMessage);
            Assert.Equal(500, result.RowCount);
        }

        [Fact]
        public void TableCopyResult_Constructor_ThrowsOnNullSourceTable()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new TableCopyResult(true, null!, "Dest", 0, 0));
        }

        [Fact]
        public void TableCopyResult_Constructor_ThrowsOnNullDestinationTable()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new TableCopyResult(true, "Source", null!, 0, 0));
        }

        [Fact]
        public void TableCopyOptions_WithDefaults_InitializesCorrectly()
        {
            var options = new TableCopyOptions();
            Assert.True(options.TruncateDestination);
            Assert.Equal(1000, options.BatchSize);
            Assert.NotNull(options.ColumnMappingOverrides);
            Assert.Empty(options.ColumnMappingOverrides);
        }

        [Fact]
        public void TableCopyOptions_WithCustomValues_InitializesCorrectly()
        {
            var overrides = new Dictionary<string, string>
            {
                { "OldCol", "NewCol" },
                { "SourceId", "TargetId" }
            };
            var options = new TableCopyOptions(false, 2500, overrides);

            Assert.False(options.TruncateDestination);
            Assert.Equal(2500, options.BatchSize);
            Assert.Equal(2, options.ColumnMappingOverrides.Count);
        }

        [Fact]
        public void TableCopyOptions_Constructor_ThrowsOnZeroBatchSize()
        {
            Assert.Throws<ArgumentException>(() => new TableCopyOptions(batchSize: 0));
        }

        [Fact]
        public void TableCopyOptions_Constructor_ThrowsOnNegativeBatchSize()
        {
            Assert.Throws<ArgumentException>(() => new TableCopyOptions(batchSize: -1));
        }

        [Fact]
        public void TableCopyOptions_Constructor_DefaultsEmptyDictionary()
        {
            var options = new TableCopyOptions();
            Assert.NotNull(options.ColumnMappingOverrides);
            Assert.Empty(options.ColumnMappingOverrides);
        }

        [Fact]
        public void StoredProcedureDefinition_WithoutParameters_CreatesCorrectly()
        {
            var definition = new StoredProcedureDefinition("sp_UpdateStats");
            Assert.Equal("sp_UpdateStats", definition.ProcedureName);
            Assert.NotNull(definition.Parameters);
            Assert.Empty(definition.Parameters);
        }

        [Fact]
        public void StoredProcedureDefinition_WithParameters_CreatesCorrectly()
        {
            var parameters = new Dictionary<string, object?>
            {
                { "@UserId", 42 },
                { "@IsActive", true },
                { "@LastUpdated", new DateTime(2024, 1, 15) }
            };
            var definition = new StoredProcedureDefinition("sp_UpdateUser", parameters);

            Assert.Equal("sp_UpdateUser", definition.ProcedureName);
            Assert.Equal(3, definition.Parameters.Count);
            Assert.Equal(42, definition.Parameters["@UserId"]);
            Assert.True((bool)definition.Parameters["@IsActive"]!);
        }

        [Fact]
        public void StoredProcedureDefinition_Constructor_ThrowsOnNullProcedureName()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new StoredProcedureDefinition(null!));
        }

        [Fact]
        public void StoredProcedureDefinition_Constructor_DefaultsEmptyParameters()
        {
            var definition = new StoredProcedureDefinition("sp_Test");
            Assert.NotNull(definition.Parameters);
            Assert.Empty(definition.Parameters);
        }

        [Fact]
        public void StoredProcedureResult_SuccessfulExecution_CreatesCorrectly()
        {
            var result = new StoredProcedureResult(true, "sp_UpdateStats", 150, null);
            Assert.True(result.Success);
            Assert.Equal("sp_UpdateStats", result.ProcedureName);
            Assert.Equal(150, result.RowsAffected);
            Assert.Null(result.ErrorMessage);
        }

        [Fact]
        public void StoredProcedureResult_FailedExecution_CreatesWithError()
        {
            var result = new StoredProcedureResult(false, "sp_UpdateStats", 0, "Invalid parameter value");
            Assert.False(result.Success);
            Assert.Equal("Invalid parameter value", result.ErrorMessage);
        }

        [Fact]
        public void StoredProcedureResult_Constructor_ThrowsOnNullProcedureName()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new StoredProcedureResult(true, null!, 0));
        }

        [Fact]
        public void EtlExecutionPlan_WithMultipleTables_CreatesCorrectly()
        {
            var tableCopies = new List<(string SourceTable, string DestinationTable, TableCopyOptions Options)>
            {
                ("Source.Users", "Target.Users", new TableCopyOptions(batchSize: 5000)),
                ("Source.Orders", "Target.Orders", new TableCopyOptions(batchSize: 10000))
            };
            var storedProcedures = new List<StoredProcedureDefinition>
            {
                new("sp_UpdateStatistics"),
                new("sp_RebuildIndexes", new Dictionary<string, object?> { { "@TableName", "Users" } })
            };

            var plan = new EtlExecutionPlan(tableCopies, storedProcedures);
            Assert.Equal(2, plan.TableCopies.Count);
            Assert.Equal(2, plan.StoredProcedures.Count);
        }

        [Fact]
        public void EtlExecutionPlan_Empty_CreatesCorrectly()
        {
            var plan = new EtlExecutionPlan();
            Assert.NotNull(plan.TableCopies);
            Assert.NotNull(plan.StoredProcedures);
            Assert.Empty(plan.TableCopies);
            Assert.Empty(plan.StoredProcedures);
        }

        [Fact]
        public void EtlExecutionResult_Successful_CreatesCorrectly()
        {
            var tableResults = new List<TableCopyResult>
            {
                new(true, "Users", "Users", 1000, 5000),
                new(true, "Orders", "Orders", 2500, 8000)
            };
            var spResults = new List<StoredProcedureResult>
            {
                new(true, "sp_UpdateStats", 10)
            };

            var result = new EtlExecutionResult(true, tableResults, spResults, null);
            Assert.True(result.Success);
            Assert.Equal(2, result.TableCopyResults.Count);
            Assert.Single(result.StoredProcedureResults);
            Assert.Null(result.ErrorMessage);
        }

        [Fact]
        public void EtlExecutionResult_Failed_CreatesWithError()
        {
            var tableResults = new List<TableCopyResult>
            {
                new(false, "Users", "Users", 500, 15000, "Connection timeout")
            };

            var result = new EtlExecutionResult(false, tableResults, null, "Failed to copy Users table");
            Assert.False(result.Success);
            Assert.Single(result.TableCopyResults);
            Assert.NotNull(result.StoredProcedureResults); // Should be empty list, not null
            Assert.Empty(result.StoredProcedureResults);
            Assert.Equal("Failed to copy Users table", result.ErrorMessage);
        }

        [Fact]
        public void EtlExecutionResult_DefaultConstructor_HasEmptyCollections()
        {
            var result = new EtlExecutionResult(success: true);
            Assert.True(result.Success);
            Assert.NotNull(result.TableCopyResults);
            Assert.NotNull(result.StoredProcedureResults);
            Assert.Empty(result.TableCopyResults);
            Assert.Empty(result.StoredProcedureResults);
            Assert.Null(result.ErrorMessage);
        }

        [Fact]
        public void TableCopyResult_WithZeroRows_CreatesCorrectly()
        {
            var result = new TableCopyResult(true, "EmptyTable", "EmptyTable", 0, 100);
            Assert.Equal(0, result.RowCount);
            Assert.True(result.Success);
        }

        [Fact]
        public void TableCopyResult_WithLargeRowCount_CreatesCorrectly()
        {
            var result = new TableCopyResult(true, "LargeTable", "LargeTable", 1000000, 300000);
            Assert.Equal(1000000, result.RowCount);
            Assert.Equal(300000, result.DurationMs);
        }

        [Fact]
        public void StoredProcedureDefinition_WithComplexParameters_CreatesCorrectly()
        {
            var parameters = new Dictionary<string, object?>
            {
                { "@IntParam", 42 },
                { "@StringParam", "test" },
                { "@BoolParam", true },
                { "@DecimalParam", 99.99m },
                { "@NullParam", null }
            };

            var definition = new StoredProcedureDefinition("sp_Test", parameters);
            Assert.Equal(5, definition.Parameters.Count);
            Assert.Null(definition.Parameters["@NullParam"]);
        }

        [Fact]
        public void TableCopyOptions_BoundaryBatchSize_CreatesCorrectly()
        {
            var options = new TableCopyOptions(batchSize: 1);
            Assert.Equal(1, options.BatchSize);

            var options2 = new TableCopyOptions(batchSize: int.MaxValue);
            Assert.Equal(int.MaxValue, options2.BatchSize);
        }

        [Fact]
        public void EtlExecutionResult_CanAddResultsAfterCreation()
        {
            var result = new EtlExecutionResult(true);
            Assert.Empty(result.TableCopyResults);

            // The IList should allow adding
            result.TableCopyResults.Add(new TableCopyResult(true, "Source", "Dest", 100, 1000));
            Assert.Single(result.TableCopyResults);
        }

        [Fact]
        public void ColumnMapping_PreservesCaseSensitivity()
        {
            var mapping = new ColumnMapping("SourceColumn", "TargetColumn");
            Assert.Equal("SourceColumn", mapping.SourceColumn);
            Assert.Equal("TargetColumn", mapping.DestinationColumn);
        }

        [Fact]
        public void StoredProcedureDefinition_WithSchemaQualifiedName()
        {
            var definition = new StoredProcedureDefinition("dbo.sp_Test");
            Assert.Equal("dbo.sp_Test", definition.ProcedureName);
        }

        [Fact]
        public void TableCopyResult_WithSchemaQualifiedTableNames()
        {
            var result = new TableCopyResult(true, "dbo.[Users]", "[dbo].[Users]", 100, 1000);
            Assert.Equal("dbo.[Users]", result.SourceTable);
            Assert.Equal("[dbo].[Users]", result.DestinationTable);
        }
    }
}
