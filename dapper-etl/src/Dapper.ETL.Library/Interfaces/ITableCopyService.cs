using System.Threading;
using System.Threading.Tasks;
using Dapper.ETL.Library.Models;

namespace Dapper.ETL.Library.Interfaces;

/// <summary>
///     Interface for copying data between tables.
/// </summary>
public interface ITableCopyService {
    /// <summary>
    ///     Copies data from a source table to a destination table asynchronously.
    /// </summary>
    /// <param name="sourceTable">The name of the source table.</param>
    /// <param name="destinationTable">The name of the destination table.</param>
    /// <param name="options">Options for the table copy operation.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The result of the table copy operation.</returns>
    Task<TableCopyResult> CopyTableAsync(
        string sourceTable,
        string destinationTable,
        TableCopyOptions options,
        CancellationToken cancellationToken = default);
}