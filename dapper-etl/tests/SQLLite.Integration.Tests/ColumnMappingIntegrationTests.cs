using Dapper.ETL.Library.Implementation;
using Dapper.ETL.Library.Models;

namespace SQLLite.Integration.Tests;

/// <summary>
/// Integration tests for column mapping with a real SQLite database.
/// </summary>
public class ColumnMappingIntegrationTests : IAsyncLifetime {
    private readonly SqliteFixture _fixture = new();

    public async Task InitializeAsync() {
        await _fixture.InitializeAsync();
    }

    public async Task DisposeAsync() {
        await _fixture.DisposeAsync();
    }

    [Fact]
    public async Task ColumnMapper_WithCaseInsensitiveMatching_MapsProperly() {
        // Arrange
        await _fixture.InsertSourceDataAsync(1, "TestName", 100, "TestDesc");

        var mapper = new ColumnMapper();
        var sourceColumns = new[] { "Id", "Name", "Value", "Description" };
        var destColumns = new[] { "id", "name", "value", "description" }; // Different case

        // Act
        var mappings = mapper.GetMapping(sourceColumns, destColumns);

        // Assert
        var columnMappings = mappings as ColumnMapping[] ?? mappings.ToArray();
        Assert.NotEmpty(columnMappings);
        foreach (var mapping in columnMappings) {
            Assert.NotNull(mapping.SourceColumn);
            Assert.NotNull(mapping.DestinationColumn);
        }
    }

    [Fact]
    public async Task ColumnMapper_WithPartialColumns_MapsAvailableColumns() {
        // Arrange
        await _fixture.InsertSourceDataAsync(1, "TestName", 100);

        var mapper = new ColumnMapper();
        var sourceColumns = new[] { "Id", "Name", "Value", "Description" };
        var destColumns = new[] { "Id", "Name", "Value" }; // Missing Description

        // Act
        var mappings = mapper.GetMapping(sourceColumns, destColumns).ToList();

        // Assert
        Assert.NotEmpty(mappings);
        // Should map the 3 matching columns
        Assert.True(mappings.Count >= 3);
    }

    [Fact]
    public void ColumnMapper_GetSelectClause_GeneratesCorrectSQL() {
        // Arrange
        var mapper = new ColumnMapper();
        var mappings = new[] {
            new ColumnMapping("Id", "Id"),
            new ColumnMapping("Name", "Name"),
            new ColumnMapping("Value", "Value")
        };

        // Act
        var selectClause = mapper.GetSelectClause(mappings);

        // Assert
        Assert.NotNull(selectClause);
        Assert.Contains("[Id]", selectClause);
        Assert.Contains("[Name]", selectClause);
        Assert.Contains("[Value]", selectClause);
    }

    [Fact]
    public void ColumnMapper_GetInsertClause_GeneratesCorrectSQL() {
        // Arrange
        var mapper = new ColumnMapper();
        var mappings = new[] {
            new ColumnMapping("Id", "Id"),
            new ColumnMapping("Name", "Name"),
            new ColumnMapping("Value", "Value")
        };

        // Act
        var insertClause = mapper.GetInsertClause(mappings);

        // Assert
        Assert.NotNull(insertClause);
        Assert.Contains("[Id]", insertClause);
        Assert.Contains("[Name]", insertClause);
        Assert.Contains("[Value]", insertClause);
    }

    [Fact]
    public void ColumnMapper_WithEmptyMappings_ReturnsEmptyClause() {
        // Arrange
        var mapper = new ColumnMapper();
        var mappings = Array.Empty<ColumnMapping>();

        // Act
        var selectClause = mapper.GetSelectClause(mappings);
        var insertClause = mapper.GetInsertClause(mappings);

        // Assert
        Assert.Empty(selectClause);
        Assert.Empty(insertClause);
    }
}