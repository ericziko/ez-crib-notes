namespace Dapper.ETL.Tests
{
    using Dapper.ETL.Library.Models;
    using Xunit;

    /// <summary>
    /// Tests verifying ColumnMapping.SelectExpression behaviour.
    /// </summary>
    public class ColumnMappingTransformTests
    {
        [Fact]
        public void SelectExpression_DefaultsToNull_WhenNotSet()
        {
            // Arrange & Act
            var mapping = new ColumnMapping("EmailAddress", "Email");

            // Assert
            Assert.Null(mapping.SelectExpression);
        }

        [Fact]
        public void SelectExpression_CanBeSetToSqlLowerFunction()
        {
            // Arrange
            var mapping = new ColumnMapping("EmailAddress", "Email");

            // Act
            mapping.SelectExpression = "LOWER(EmailAddress)";

            // Assert
            Assert.Equal("LOWER(EmailAddress)", mapping.SelectExpression);
        }

        [Fact]
        public void SelectExpression_CanBeSetToSqlTrimFunction()
        {
            // Arrange
            var mapping = new ColumnMapping("FirstName", "FirstName");

            // Act
            mapping.SelectExpression = "TRIM(FirstName)";

            // Assert
            Assert.Equal("TRIM(FirstName)", mapping.SelectExpression);
        }

        [Fact]
        public void MultipleColumnMappings_EachHasIndependentSelectExpression()
        {
            // Arrange
            var lowerMapping = new ColumnMapping("EmailAddress", "Email");
            var trimMapping = new ColumnMapping("FirstName", "FirstName");
            var noTransformMapping = new ColumnMapping("Id", "Id");

            // Act
            lowerMapping.SelectExpression = "LOWER(EmailAddress)";
            trimMapping.SelectExpression = "TRIM(FirstName)";
            // noTransformMapping left as null

            // Assert
            Assert.Equal("LOWER(EmailAddress)", lowerMapping.SelectExpression);
            Assert.Equal("TRIM(FirstName)", trimMapping.SelectExpression);
            Assert.Null(noTransformMapping.SelectExpression);
        }

        [Fact]
        public void ColumnMapping_NullSelectExpression_MeansColumnCopiedAsIs()
        {
            // Arrange
            var mapping = new ColumnMapping("ProductCode", "ProductCode");

            // Act - leave SelectExpression as null (no transform)

            // Assert: null SelectExpression signals "copy column as-is"
            Assert.Null(mapping.SelectExpression);
            Assert.Equal("ProductCode", mapping.SourceColumn);
            Assert.Equal("ProductCode", mapping.DestinationColumn);
        }
    }
}
