using System.Threading;
using System.Threading.Tasks;
using Dapper.ETL.Library.Models;

namespace Dapper.ETL.Library.Interfaces;

/// <summary>
///     Interface for executing stored procedures.
/// </summary>
public interface IStoredProcedureService {
    /// <summary>
    ///     Executes a stored procedure asynchronously.
    /// </summary>
    /// <param name="procedureDefinition">The definition of the stored procedure to execute.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The result of the stored procedure execution.</returns>
    Task<StoredProcedureResult> ExecuteAsync(
        StoredProcedureDefinition procedureDefinition,
        CancellationToken cancellationToken = default);
}