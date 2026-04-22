using System.Collections.Generic;
using System.Linq;

namespace Dapper.ETL.Library.Models;

/// <summary>
///     Represents a plan for executing ETL operations.
/// </summary>
public class EtlExecutionPlan {
    /// <summary>
    ///     Initializes a new instance of the <see cref="EtlExecutionPlan" /> class.
    /// </summary>
    /// <param name="tableCopies">The table copy operations to perform.</param>
    /// <param name="storedProcedures">The stored procedures to execute.</param>
    public EtlExecutionPlan(
        IEnumerable<(string SourceTable, string DestinationTable, TableCopyOptions Options)>? tableCopies = null,
        IEnumerable<StoredProcedureDefinition>? storedProcedures = null) {
        TableCopies = tableCopies?.ToList() ?? new List<(string, string, TableCopyOptions)>();
        StoredProcedures = storedProcedures?.ToList() ?? new List<StoredProcedureDefinition>();
    }

    /// <summary>
    ///     Gets the list of table copy operations.
    /// </summary>
    public IList<(string SourceTable, string DestinationTable, TableCopyOptions Options)> TableCopies { get; }

    /// <summary>
    ///     Gets the list of stored procedures to execute.
    /// </summary>
    public IList<StoredProcedureDefinition> StoredProcedures { get; }
}