namespace ChatUp.Pages.Admin
{
    /// <summary>A click on the audit dashboard: open the activity list with these filters (dates are Philippine-time days).</summary>
    public sealed record AuditDrill(
        string? UserName,
        string? Module,
        string? Category,
        string? Action,
        bool FailedOnly,
        DateTime From,
        DateTime To);
}
