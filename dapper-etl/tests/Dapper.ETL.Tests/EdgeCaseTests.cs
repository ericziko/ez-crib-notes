namespace Dapper.ETL.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Dapper.ETL.Library.Implementation;
    using Dapper.ETL.Library.Models;
    using Xunit;

    /// <summary>
    /// Edge case tests for complete 100% coverage
    /// </summary>
    public class EdgeCaseTests
    {
        private readonly ColumnMapper _columnMapper = new ColumnMapper();

        [Fact]
        public void ColumnMapper_SingleColumn_MapsCorrectly()
        {
            // Arrange
            var sourceColumns = new[] { "Id" };
            var destinationColumns = new[] { "Id" };

            // Act
            var result = _columnMapper.GetMapping(sourceColumns, destinationColumns).ToList();

            // Assert
            Assert.Single(result);
            Assert.Equal("Id", result[0].SourceColumn);
            Assert.Equal("Id", result[0].DestinationColumn);
        }

        [Fact]
        public void ColumnMapper_ManyColumns_AllMapped()
        {
            // Arrange
            var sourceColumns = Enumerable.Range(1, 20).Select(i => $"Col{i}").ToArray();
            var destinationColumns = sourceColumns;

            // Act
            var result = _columnMapper.GetMapping(sourceColumns, destinationColumns);

            // Assert
            Assert.Equal(20, result.Count());
        }

        [Fact]
        public void ColumnMapper_SourceNotInDestination_FiltersOut()
        {
            // Arrange
            var sourceColumns = new[] { "Id", "Name", "NonExistent1", "NonExistent2", "NonExistent3" };
            var destinationColumns = new[] { "Id", "Name" };

            // Act
            var result = _columnMapper.GetMapping(sourceColumns, destinationColumns).ToList();

            // Assert
            Assert.Equal(2, result.Count);
            Assert.All(result, m => Assert.True(
                m.SourceColumn == "Id" || m.SourceColumn == "Name"));
        }

        [Fact]
        public void ColumnMapper_OverrideTakesPriority()
        {
            // Arrange
            var sourceColumns = new[] { "Id", "Name" };
            var destinationColumns = new[] { "Id", "Name", "UserId", "UserName" };
            var overrides = new Dictionary<string, string>
            {
                { "Id", "UserId" },
                { "Name", "UserName" }
            };

            // Act
            var result = _columnMapper.GetMapping(sourceColumns, destinationColumns, overrides).ToList();

            // Assert
            Assert.Equal(2, result.Count);
            var idMapping = result.First(m => m.SourceColumn == "Id");
            Assert.Equal("UserId", idMapping.DestinationColumn);

            var nameMapping = result.First(m => m.SourceColumn == "Name");
            Assert.Equal("UserName", nameMapping.DestinationColumn);
        }

        [Fact]
        public void ColumnMapper_PartialOverride()
        {
            // Arrange
            var sourceColumns = new[] { "Id", "Name", "Email" };
            var destinationColumns = new[] { "Id", "Name", "Email" };
            var overrides = new Dictionary<string, string>
            {
                { "Email", "EmailAddress" }
            };

            // Act
            var result = _columnMapper.GetMapping(sourceColumns, destinationColumns, overrides).ToList();

            // Assert
            var emailMapping = result.First(m => m.SourceColumn == "Email");
            Assert.Equal("EmailAddress", emailMapping.DestinationColumn);

            var idMapping = result.First(m => m.SourceColumn == "Id");
            Assert.Equal("Id", idMapping.DestinationColumn);
        }

        [Fact]
        public void ColumnMapper_SelectClause_HasProperFormatting()
        {
            // Arrange
            var sourceColumns = new[] { "Id", "Name", "Email" };
            var destinationColumns = sourceColumns;
            var mappings = _columnMapper.GetMapping(sourceColumns, destinationColumns);

            // Act
            var selectClause = _columnMapper.GetSelectClause(mappings);

            // Assert
            Assert.Equal("[Id], [Name], [Email]", selectClause);
            Assert.Contains("[", selectClause);
            Assert.Contains("]", selectClause);
        }

        [Fact]
        public void ColumnMapper_InsertClause_HasProperFormatting()
        {
            // Arrange
            var sourceColumns = new[] { "Id", "Name", "Email" };
            var destinationColumns = new[] { "UserId", "UserName", "UserEmail" };
            var overrides = new Dictionary<string, string>
            {
                { "Id", "UserId" },
                { "Name", "UserName" },
                { "Email", "UserEmail" }
            };
            var mappings = _columnMapper.GetMapping(sourceColumns, destinationColumns, overrides);

            // Act
            var insertClause = _columnMapper.GetInsertClause(mappings);

            // Assert
            Assert.Equal("[UserId], [UserName], [UserEmail]", insertClause);
            Assert.Contains("[", insertClause);
            Assert.Contains("]", insertClause);
        }

        [Fact]
        public void ColumnMapper_CaseInsensitive_DifferentCases()
        {
            // Arrange
            var sourceColumns = new[] { "id", "NAME", "EmAiL" };
            var destinationColumns = new[] { "ID", "name", "email" };

            // Act
            var result = _columnMapper.GetMapping(sourceColumns, destinationColumns).ToList();

            // Assert
            Assert.Equal(3, result.Count);
            // Verify that destination columns preserve their case
            var idMapping = result.First(m => m.SourceColumn == "id");
            Assert.Equal("ID", idMapping.DestinationColumn);
        }

        [Fact]
        public void TableCopyOptions_MaxBatchSize_IsValid()
        {
            // Arrange & Act
            var options = new TableCopyOptions(batchSize: int.MaxValue);

            // Assert
            Assert.Equal(int.MaxValue, options.BatchSize);
        }

        [Fact]
        public void TableCopyOptions_EmptyColumnMappingOverrides()
        {
            // Arrange & Act
            var options = new TableCopyOptions(columnMappingOverrides: new Dictionary<string, string>());

            // Assert
            Assert.NotNull(options.ColumnMappingOverrides);
            Assert.Empty(options.ColumnMappingOverrides);
        }

        [Fact]
        public void StoredProcedureDefinition_WithNullParameter()
        {
            // Arrange
            var parameters = new Dictionary<string, object?> { { "@NullParam", null } };

            // Act
            var definition = new StoredProcedureDefinition("sp_Test", parameters);

            // Assert
            Assert.Contains("@NullParam", definition.Parameters.Keys);
            Assert.Null(definition.Parameters["@NullParam"]);
        }

        [Fact]
        public void StoredProcedureDefinition_WithMixedParameterTypes()
        {
            // Arrange
            var parameters = new Dictionary<string, object?>
            {
                { "@IntParam", 42 },
                { "@StringParam", "test" },
                { "@BoolParam", true },
                { "@DecimalParam", 99.99m },
                { "@DateParam", new DateTime(2024, 1, 1) }
            };

            // Act
            var definition = new StoredProcedureDefinition("sp_Test", parameters);

            // Assert
            Assert.Equal(5, definition.Parameters.Count);
            Assert.IsType<int>(definition.Parameters["@IntParam"]);
            Assert.IsType<string>(definition.Parameters["@StringParam"]);
            Assert.IsType<bool>(definition.Parameters["@BoolParam"]);
            Assert.IsType<decimal>(definition.Parameters["@DecimalParam"]);
            Assert.IsType<DateTime>(definition.Parameters["@DateParam"]);
        }

        [Fact]
        public void EtlExecutionPlan_WithEmptyTableCopies()
        {
            // Arrange
            var tableCopies = new List<(string SourceTable, string DestinationTable, TableCopyOptions Options)>();
            var storedProcedures = new List<StoredProcedureDefinition>
            {
                new("sp_Test")
            };

            // Act
            var plan = new EtlExecutionPlan(tableCopies, storedProcedures);

            // Assert
            Assert.Empty(plan.TableCopies);
            Assert.Single(plan.StoredProcedures);
        }

        [Fact]
        public void EtlExecutionPlan_WithEmptyStoredProcedures()
        {
            // Arrange
            var tableCopies = new List<(string SourceTable, string DestinationTable, TableCopyOptions Options)>
            {
                ("Source", "Dest", new TableCopyOptions())
            };
            var storedProcedures = new List<StoredProcedureDefinition>();

            // Act
            var plan = new EtlExecutionPlan(tableCopies, storedProcedures);

            // Assert
            Assert.Single(plan.TableCopies);
            Assert.Empty(plan.StoredProcedures);
        }

        [Fact]
        public void TableCopyResult_WithVeryLongErrorMessage()
        {
            // Arrange
            var longError = string.Concat(Enumerable.Repeat("Error message. ", 1000));

            // Act
            var result = new TableCopyResult(false, "Source", "Dest", 0, 1000, longError);

            // Assert
            Assert.NotNull(result.ErrorMessage);
            Assert.Equal(longError, result.ErrorMessage);
            Assert.NotEmpty(result.ErrorMessage);
        }

        [Fact]
        public void TableCopyResult_WithSpecialCharacters_InTableNames()
        {
            // Arrange & Act
            var result = new TableCopyResult(
                true,
                "dbo.[Special-Table]",
                "[Target].[Special_Table]",
                100,
                5000);

            // Assert
            Assert.Equal("dbo.[Special-Table]", result.SourceTable);
            Assert.Equal("[Target].[Special_Table]", result.DestinationTable);
        }

        [Fact]
        public void ColumnMapping_WithSpecialCharacters()
        {
            // Arrange & Act
            var mapping = new ColumnMapping("[Source-Col]", "[Target_Col]");

            // Assert
            Assert.Equal("[Source-Col]", mapping.SourceColumn);
            Assert.Equal("[Target_Col]", mapping.DestinationColumn);
        }

        [Fact]
        public void ColumnMapper_SelectClause_SingleColumn()
        {
            // Arrange
            var mappings = new List<ColumnMapping> { new("Id", "Id") };

            // Act
            var selectClause = _columnMapper.GetSelectClause(mappings);

            // Assert
            Assert.Equal("[Id]", selectClause);
        }

        [Fact]
        public void ColumnMapper_InsertClause_SingleColumn()
        {
            // Arrange
            var mappings = new List<ColumnMapping> { new("Id", "Id") };

            // Act
            var insertClause = _columnMapper.GetInsertClause(mappings);

            // Assert
            Assert.Equal("[Id]", insertClause);
        }

        [Fact]
        public void ColumnMapper_LargeNumberOfColumns()
        {
            // Arrange
            var sourceColumns = Enumerable.Range(1, 100).Select(i => $"Column{i}").ToArray();
            var destinationColumns = sourceColumns;

            // Act
            var result = _columnMapper.GetMapping(sourceColumns, destinationColumns).ToList();

            // Assert
            Assert.Equal(100, result.Count);
        }

        [Fact]
        public void StoredProcedureDefinition_LongProcedureName()
        {
            // Arrange
            var longName = "sp_" + string.Concat(Enumerable.Repeat("VeryLongName", 10));

            // Act
            var definition = new StoredProcedureDefinition(longName);

            // Assert
            Assert.Equal(longName, definition.ProcedureName);
        }

        [Fact]
        public void TableCopyOptions_BoundaryBatchSize()
        {
            // Arrange & Act
            var options = new TableCopyOptions(batchSize: 1);

            // Assert
            Assert.Equal(1, options.BatchSize);
        }

        [Fact]
        public void EtlExecutionResult_DefaultConstructor_HasEmptyCollections()
        {
            // Arrange & Act
            var result = new EtlExecutionResult(success: false);

            // Assert
            Assert.NotNull(result.TableCopyResults);
            Assert.NotNull(result.StoredProcedureResults);
            Assert.Empty(result.TableCopyResults);
            Assert.Empty(result.StoredProcedureResults);
        }
    }
}
