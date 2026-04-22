namespace Dapper.ETL.Orchestrator.Models;

/// <summary>
///     Represents the result of a validation operation.
/// </summary>
public class ValidationResult {
    /// <summary>
    ///     Gets or sets a value indicating whether validation passed.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    ///     Gets or sets the number of tables checked.
    /// </summary>
    public int TablesChecked { get; set; }

    /// <summary>
    ///     Gets or sets the number of tables that passed validation.
    /// </summary>
    public int TablesPassed { get; set; }

    /// <summary>
    ///     Gets or sets the number of tables that failed validation.
    /// </summary>
    public int TablesFailed { get; set; }

    /// <summary>
    ///     Gets or sets an optional error message if validation failed.
    /// </summary>
    public string? ErrorMessage { get; set; }
}