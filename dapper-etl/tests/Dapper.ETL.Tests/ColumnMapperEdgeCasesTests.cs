using System;
using System.Collections.Generic;
using System.Linq;
using Dapper.ETL.Library.Implementation;
using Dapper.ETL.Library.Models;
using Xunit;

namespace Dapper.ETL.Tests;

/// <summary>
///     Edge case tests for ColumnMapper covering all code paths
/// </summary>
public class ColumnMapperEdgeCasesTests {
    private readonly ColumnMapper _mapper = new();

    [Fact]
    public void GetMapping_WithDuplicateSourceColumns_HandlesCorrectly() {
        var sourceColumns = new[] { "Id", "Id", "Name" };
        var destinationColumns = new[] { "Id", "Name" };

        var result = _mapper.GetMapping(sourceColumns, destinationColumns).ToList();

        Assert.Equal(3, result.Count);
        Assert.Equal(2, result.Count(m => m.SourceColumn == "Id"));
    }

    [Fact]
    public void GetMapping_WithDuplicateDestinationColumns_MatchesSourceCorrectly() {
        var sourceColumns = new[] { "Id", "Name" };
        var destinationColumns = new[] { "Id", "Id", "Name" };

        var result = _mapper.GetMapping(sourceColumns, destinationColumns).ToList();

        Assert.Equal(2, result.Count);
        var idMappings = result.Where(m => m.SourceColumn == "Id").ToList();
        Assert.Single(idMappings);
        Assert.Equal("Id", idMappings[0].DestinationColumn);
    }

    [Fact]
    public void GetMapping_WithOverrideForNonExistentSourceColumn_IgnoresOverride() {
        var sourceColumns = new[] { "Id", "Name" };
        var destinationColumns = new[] { "UserId", "UserName" };
        var overrides = new Dictionary<string, string> {
            { "NonExistent", "SomeColumn" },
            { "Id", "UserId" }
        };

        var result = _mapper.GetMapping(sourceColumns, destinationColumns, overrides).ToList();

        Assert.Single(result);
        Assert.Equal("Id", result[0].SourceColumn);
        Assert.Equal("UserId", result[0].DestinationColumn);
    }

    [Fact]
    public void GetMapping_WithEmptyOverridesDict_UsesExactMatching() {
        var sourceColumns = new[] { "Id", "Name", "Email" };
        var destinationColumns = new[] { "Id", "Name", "Email" };
        var overrides = new Dictionary<string, string>();

        var result = _mapper.GetMapping(sourceColumns, destinationColumns, overrides).ToList();

        Assert.Equal(3, result.Count);
        Assert.All(result, m => Assert.Equal(m.SourceColumn, m.DestinationColumn));
    }

    [Fact]
    public void GetMapping_WithMixedCaseInOverrides() {
        var sourceColumns = new[] { "id", "name" };
        var destinationColumns = new[] { "UserId", "UserName" };
        var overrides = new Dictionary<string, string> {
            { "id", "UserId" },
            { "name", "UserName" }
        };

        var result = _mapper.GetMapping(sourceColumns, destinationColumns, overrides).ToList();

        Assert.Equal(2, result.Count);
        var idMapping = result.First(m => m.SourceColumn == "id");
        Assert.Equal("UserId", idMapping.DestinationColumn);
    }

    [Fact]
    public void GetSelectClause_WithSingleColumnMapping() {
        var mappings = new[] { new ColumnMapping("Id", "Id") };
        var result = _mapper.GetSelectClause(mappings);

        Assert.Equal("[Id]", result);
        Assert.StartsWith("[", result);
        Assert.EndsWith("]", result);
    }

    [Fact]
    public void GetSelectClause_WithManyColumns_ProperlyFormatted() {
        var columns = Enumerable.Range(1, 10).Select(i => $"Col{i}").ToArray();
        var mappings = columns.Select(c => new ColumnMapping(c, c)).ToArray();

        var result = _mapper.GetSelectClause(mappings);

        Assert.Contains("[Col1]", result);
        Assert.Contains("[Col10]", result);
        var count = result.Split(new[] { "," }, StringSplitOptions.None).Length;
        Assert.Equal(10, count);
    }

    [Fact]
    public void GetSelectClause_WithSpecialCharactersInNames() {
        var mappings = new[] {
            new ColumnMapping("[Col1]", "[Col1]"),
            new ColumnMapping("Col-2", "Col-2")
        };

        var result = _mapper.GetSelectClause(mappings);

        Assert.Contains("[[Col1]]", result);
        Assert.Contains("[Col-2]", result);
    }

    [Fact]
    public void GetInsertClause_WithSingleColumnMapping() {
        var mappings = new[] { new ColumnMapping("Id", "Id") };
        var result = _mapper.GetInsertClause(mappings);

        Assert.Equal("[Id]", result);
    }

    [Fact]
    public void GetInsertClause_WithMappedColumns_UsesDestinationNames() {
        var mappings = new[] {
            new ColumnMapping("SourceId", "TargetId"),
            new ColumnMapping("SourceName", "TargetName")
        };

        var result = _mapper.GetInsertClause(mappings);

        Assert.Contains("[TargetId]", result);
        Assert.Contains("[TargetName]", result);
        Assert.DoesNotContain("SourceId", result);
        Assert.DoesNotContain("SourceName", result);
    }

    [Fact]
    public void GetInsertClause_CommaSeperatedCorrectly() {
        var mappings = new[] {
            new ColumnMapping("A", "A"),
            new ColumnMapping("B", "B"),
            new ColumnMapping("C", "C")
        };

        var result = _mapper.GetInsertClause(mappings);

        Assert.Equal("[A], [B], [C]", result);
    }

    [Fact]
    public void GetMapping_PreservesSourceColumnCaseInMapping() {
        var sourceColumns = new[] { "UserId", "userName" };
        var destinationColumns = new[] { "UserId", "userName" };

        var result = _mapper.GetMapping(sourceColumns, destinationColumns).ToList();

        Assert.Equal("UserId", result[0].SourceColumn);
        Assert.Equal("userName", result[1].SourceColumn);
    }

    [Fact]
    public void GetMapping_PreservesDestinationColumnCaseInMapping() {
        var sourceColumns = new[] { "id", "name" };
        var destinationColumns = new[] { "ID", "NAME" };

        var result = _mapper.GetMapping(sourceColumns, destinationColumns).ToList();

        Assert.Equal(2, result.Count);
        Assert.Equal("ID", result[0].DestinationColumn);
        Assert.Equal("NAME", result[1].DestinationColumn);
    }

    [Fact]
    public void GetMapping_CaseInsensitiveMatchingWithDifferentCases() {
        var sourceColumns = new[] { "ID", "NAME", "EMAIL" };
        var destinationColumns = new[] { "id", "name", "email" };

        var result = _mapper.GetMapping(sourceColumns, destinationColumns).ToList();

        Assert.Equal(3, result.Count);
        Assert.Contains(result, m => m.SourceColumn == "ID" && m.DestinationColumn == "id");
    }

    [Fact]
    public void GetSelectClause_ColumnNamesWithNumbers() {
        var mappings = new[] {
            new ColumnMapping("Col1", "Col1"),
            new ColumnMapping("Col2", "Col2"),
            new ColumnMapping("Col10", "Col10")
        };

        var result = _mapper.GetSelectClause(mappings);

        Assert.Contains("[Col1]", result);
        Assert.Contains("[Col10]", result);
    }

    [Fact]
    public void GetInsertClause_ColumnNamesWithUnderscores() {
        var mappings = new[] {
            new ColumnMapping("user_id", "user_id"),
            new ColumnMapping("first_name", "first_name")
        };

        var result = _mapper.GetInsertClause(mappings);

        Assert.Contains("[user_id]", result);
        Assert.Contains("[first_name]", result);
    }

    [Fact]
    public void GetMapping_LargeNumberOfColumns() {
        var columnCount = 50;
        var sourceColumns = Enumerable.Range(1, columnCount).Select(i => $"Col{i}").ToArray();
        var destinationColumns = sourceColumns;

        var result = _mapper.GetMapping(sourceColumns, destinationColumns).ToList();

        Assert.Equal(columnCount, result.Count);
    }

    [Fact]
    public void GetSelectClause_LargeNumberOfColumns() {
        var columnCount = 50;
        var sourceColumns = Enumerable.Range(1, columnCount).Select(i => $"Col{i}").ToArray();
        var mappings = sourceColumns.Select(c => new ColumnMapping(c, c)).ToArray();

        var result = _mapper.GetSelectClause(mappings);

        Assert.Contains("[Col1]", result);
        Assert.Contains("[Col50]", result);
        var count = result.Split(new[] { "," }, StringSplitOptions.None).Length;
        Assert.Equal(columnCount, count);
    }

    [Fact]
    public void GetMapping_WithAllColumnsOverridden() {
        var sourceColumns = new[] { "A", "B", "C" };
        var destinationColumns = new[] { "X", "Y", "Z" };
        var overrides = new Dictionary<string, string> {
            { "A", "X" },
            { "B", "Y" },
            { "C", "Z" }
        };

        var result = _mapper.GetMapping(sourceColumns, destinationColumns, overrides).ToList();

        Assert.Equal(3, result.Count);
        Assert.All(result, m => Assert.NotEqual(m.SourceColumn, m.DestinationColumn));
    }

    [Fact]
    public void GetMapping_WithPartialOverridesPartialMatches() {
        var sourceColumns = new[] { "Id", "Name", "Email", "Phone" };
        var destinationColumns = new[] { "UserId", "UserName", "Email", "Phone" };
        var overrides = new Dictionary<string, string> {
            { "Id", "UserId" },
            { "Name", "UserName" }
        };

        var result = _mapper.GetMapping(sourceColumns, destinationColumns, overrides).ToList();

        Assert.Equal(4, result.Count);
        var overriddenMappings = result.Where(m => m.SourceColumn == "Id" || m.SourceColumn == "Name").ToList();
        Assert.All(overriddenMappings, m => Assert.NotEqual(m.SourceColumn, m.DestinationColumn));
    }

    [Fact]
    public void GetSelectClause_OrderPreserved() {
        var mappings = new[] {
            new ColumnMapping("Z", "Z"),
            new ColumnMapping("A", "A"),
            new ColumnMapping("M", "M")
        };

        var result = _mapper.GetSelectClause(mappings);

        var parts = result.Split(new[] { ", " }, StringSplitOptions.None);
        Assert.Equal("[Z]", parts[0]);
        Assert.Equal("[A]", parts[1]);
        Assert.Equal("[M]", parts[2]);
    }

    [Fact]
    public void GetInsertClause_OrderPreserved() {
        var mappings = new[] {
            new ColumnMapping("X", "ColX"),
            new ColumnMapping("B", "ColB"),
            new ColumnMapping("Y", "ColY")
        };

        var result = _mapper.GetInsertClause(mappings);

        var parts = result.Split(new[] { ", " }, StringSplitOptions.None);
        Assert.Equal("[ColX]", parts[0]);
        Assert.Equal("[ColB]", parts[1]);
        Assert.Equal("[ColY]", parts[2]);
    }
}