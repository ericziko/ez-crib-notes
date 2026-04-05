namespace Dapper.ETL.Library.Models
{
    /// <summary>
    /// Controls transaction behavior for ETL operations.
    /// </summary>
    public enum EtlTransactionMode
    {
        /// <summary>
        /// All table copies in a single transaction. If any copy fails, all changes are rolled back.
        /// Ensures all-or-nothing consistency but blocks on first error.
        /// </summary>
        Atomic = 0,

        /// <summary>
        /// Each table copy in its own transaction. If one copy fails, logs the error but continues to next table.
        /// Allows partial success but may leave inconsistent state. Useful for resilience testing.
        /// </summary>
        Partial = 1
    }
}
