namespace IncidentHub.Api.Domain.Enums
{
    /// <summary>
    /// Reflects a standard incident lifecycle.
    /// Stored as int — do not reorder or remove values once the DB is seeded,
    /// as existing rows reference the ordinal positions.
    /// To rename a status, add a [Description] attribute rather than renaming the member.
    /// </summary>
    public enum IncidentStatus
    {
        New = 0,
        Investigating = 1,
        Identified = 2,
        Monitoring = 3,
        Resolved = 4
    }
}
