namespace Dapper.ETL.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Dapper.ETL.Library.Implementation;
    using Xunit;

    public class ColumnMapperTests
    {
        private readonly ColumnMapper _mapper = new ColumnMapper();

        [Fact]
        public void GetMapping_WithExactSchemaMatch_ReturnsAllMappings()
        {
            // Arrange
            var sourceColumns = new[] { "Id", "Name", "Email" };
            var destinationColumns = new[] { "Id", "Name", "Email" };

            // Act
            var result = _mapper.GetMapping(sourceColumns, destinationColumns);

            // Assert
            var mappings = result.ToList();
            Assert.Equal(3, mappings.Count);
            Assert.Equal("Id", mappings[0].SourceColumn);
            Assert.Equal("Id", mappings[0].DestinationColumn);
            Assert.Equal("Name", mappings[1].SourceColumn);
            Assert.Equal("Name", mappings[1].DestinationColumn);
            Assert.Equal("Email", mappings[2].SourceColumn);
            Assert.Equal("Email", mappings[2].DestinationColumn);
        }

        [Fact]
        public void GetMapping_WithPartialColumnMapping_ReturnsOnlyMatchingColumns()
        {
            // Arrange
            var sourceColumns = new[] { "Id", "Name", "Phone" };
            var destinationColumns = new[] { "Id", "Name" };

            // Act
            var result = _mapper.GetMapping(sourceColumns, destinationColumns);

            // Assert
            var mappings = result.ToList();
            Assert.Equal(2, mappings.Count);
            Assert.Contains(mappings, m => m.SourceColumn == "Id");
            Assert.Contains(mappings, m => m.SourceColumn == "Name");
            Assert.DoesNotContain(mappings, m => m.SourceColumn == "Phone");
        }

        [Fact]
        public void GetMapping_WithColumnOrderDifference_MatchesByName()
        {
            // Arrange
            var sourceColumns = new[] { "Name", "Id", "Email" };
            var destinationColumns = new[] { "Id", "Email", "Name" };

            // Act
            var result = _mapper.GetMapping(sourceColumns, destinationColumns);

            // Assert
            var mappings = result.ToList();
            Assert.Equal(3, mappings.Count);
            var mapping1 = mappings.FirstOrDefault(m => m.SourceColumn == "Name");
            var mapping2 = mappings.FirstOrDefault(m => m.SourceColumn == "Id");
            var mapping3 = mappings.FirstOrDefault(m => m.SourceColumn == "Email");

            Assert.NotNull(mapping1);
            Assert.NotNull(mapping2);
            Assert.NotNull(mapping3);
        }

        [Fact]
        public void GetMapping_WithCaseInsensitiveMatch_FindsColumns()
        {
            // Arrange
            var sourceColumns = new[] { "id", "name" };
            var destinationColumns = new[] { "ID", "NAME" };

            // Act
            var result = _mapper.GetMapping(sourceColumns, destinationColumns);

            // Assert
            var mappings = result.ToList();
            Assert.Equal(2, mappings.Count);
        }

        [Fact]
        public void GetMapping_WithMappingOverrides_UsesOverrides()
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

            // Act
            var result = _mapper.GetMapping(sourceColumns, destinationColumns, overrides);

            // Assert
            var mappings = result.ToList();
            Assert.Equal(3, mappings.Count);
            Assert.Contains(mappings, m => m.SourceColumn == "Id" && m.DestinationColumn == "UserId");
            Assert.Contains(mappings, m => m.SourceColumn == "Name" && m.DestinationColumn == "UserName");
            Assert.Contains(mappings, m => m.SourceColumn == "Email" && m.DestinationColumn == "UserEmail");
        }

        [Fact]
        public void GetMapping_WithEmptySourceColumns_ReturnsEmptyMappings()
        {
            // Arrange
            var sourceColumns = new string[] { };
            var destinationColumns = new[] { "Id", "Name" };

            // Act
            var result = _mapper.GetMapping(sourceColumns, destinationColumns);

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public void GetMapping_WithEmptyDestinationColumns_ReturnsEmptyMappings()
        {
            // Arrange
            var sourceColumns = new[] { "Id", "Name" };
            var destinationColumns = new string[] { };

            // Act
            var result = _mapper.GetMapping(sourceColumns, destinationColumns);

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public void GetMapping_WithNullSourceColumns_ThrowsArgumentNullException()
        {
            // Arrange
            IEnumerable<string>? sourceColumns = null;
            var destinationColumns = new[] { "Id", "Name" };

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _mapper.GetMapping(sourceColumns!, destinationColumns));
        }

        [Fact]
        public void GetMapping_WithNullDestinationColumns_ThrowsArgumentNullException()
        {
            // Arrange
            var sourceColumns = new[] { "Id", "Name" };
            IEnumerable<string>? destinationColumns = null;

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _mapper.GetMapping(sourceColumns, destinationColumns!));
        }

        [Fact]
        public void GetSelectClause_WithMappings_ReturnsFormattedSelectList()
        {
            // Arrange
            var sourceColumns = new[] { "Id", "Name", "Email" };
            var destinationColumns = new[] { "Id", "Name", "Email" };
            var mappings = _mapper.GetMapping(sourceColumns, destinationColumns);

            // Act
            var result = _mapper.GetSelectClause(mappings);

            // Assert
            Assert.Equal("[Id], [Name], [Email]", result);
        }

        [Fact]
        public void GetSelectClause_WithEmptyMappings_ReturnsEmptyString()
        {
            // Arrange
            var mappings = new List<Dapper.ETL.Library.Models.ColumnMapping>();

            // Act
            var result = _mapper.GetSelectClause(mappings);

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public void GetSelectClause_WithNullMappings_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _mapper.GetSelectClause(null!));
        }

        [Fact]
        public void GetInsertClause_WithMappings_ReturnsFormattedColumnList()
        {
            // Arrange
            var sourceColumns = new[] { "Id", "Name", "Email" };
            var destinationColumns = new[] { "Id", "Name", "Email" };
            var mappings = _mapper.GetMapping(sourceColumns, destinationColumns);

            // Act
            var result = _mapper.GetInsertClause(mappings);

            // Assert
            Assert.Equal("[Id], [Name], [Email]", result);
        }

        [Fact]
        public void GetInsertClause_WithPartialMapping_ReturnsOnlyMappedColumns()
        {
            // Arrange
            var sourceColumns = new[] { "Id", "Name", "Phone" };
            var destinationColumns = new[] { "Id", "Name" };
            var mappings = _mapper.GetMapping(sourceColumns, destinationColumns);

            // Act
            var result = _mapper.GetInsertClause(mappings);

            // Assert
            Assert.Equal("[Id], [Name]", result);
        }

        [Fact]
        public void GetInsertClause_WithEmptyMappings_ReturnsEmptyString()
        {
            // Arrange
            var mappings = new List<Dapper.ETL.Library.Models.ColumnMapping>();

            // Act
            var result = _mapper.GetInsertClause(mappings);

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public void GetInsertClause_WithNullMappings_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _mapper.GetInsertClause(null!));
        }
    }
}
