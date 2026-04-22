namespace Dapper.ETL.Orchestrator.Models;

/// <summary>
///     Represents the result of an ETL operation.
/// </summary>
public class EtlResult {
    /// <summary>
    ///     Gets or sets a value indicating whether the operation succeeded.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    ///     Gets or sets the total number of rows copied.
    /// </summary>
    public int RowsCopied { get; set; }

    /// <summary>
    ///     Gets or sets the duration of the operation in milliseconds.
    /// </summary>
    public long Duration { get; set; }

    /// <summary>
    ///     Gets or sets an error message if the operation failed.
    /// </summary>
    public string? ErrorMessage { get; set; }
}