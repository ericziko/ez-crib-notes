using System;
using System.Collections.Generic;

namespace Dapper.ETL.Library.Models;

/// <summary>
///     Represents the definition of a stored procedure to execute.
/// </summary>
public class StoredProcedureDefinition {
    /// <summary>
    ///     Initializes a new instance of the <see cref="StoredProcedureDefinition" /> class.
    /// </summary>
    /// <param name="procedureName">The name of the stored procedure.</param>
    /// <param name="parameters">The parameters to pass to the stored procedure.</param>
    public StoredProcedureDefinition(
        string procedureName,
        IDictionary<string, object?>? parameters = null) {
        ProcedureName = procedureName ?? throw new ArgumentNullException(nameof(procedureName));
        Parameters = parameters ?? new Dictionary<string, object?>();
    }

    /// <summary>
    ///     Gets the name of the stored procedure.
    /// </summary>
    public string ProcedureName { get; }

    /// <summary>
    ///     Gets the parameters to pass to the stored procedure.
    /// </summary>
    public IDictionary<string, object?> Parameters { get; }
}