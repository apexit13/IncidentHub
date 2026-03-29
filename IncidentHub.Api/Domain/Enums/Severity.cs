namespace IncidentHub.Api.Domain.Enums
{
    /// <summary>
    /// Stored as int in SQL. Ordered low-to-high so ORDER BY Severity DESC
    /// surfaces the most critical incidents first without a CASE expression.
    /// </summary>
    public enum Severity
    {
        Low = 0,
        Medium = 1,
        High = 2,
        Critical = 3
    }
}
