namespace PrintGrid.Modules.Customer.Application.Auth;

/// <summary>
/// Demo role mapping: normal registrations are customers; special seed accounts
/// represent lab / hub / ops personas so the teacher can log in as each role
/// and see that role's own area. In production this maps to a real user/role store.
/// </summary>
public static class DemoRoles
{
    public const string LabEmail = "lab@printgrid.dev";
    public const string HubEmail = "qc@printgrid.dev";
    public const string OpsEmail = "ops@printgrid.dev";

    public static IReadOnlyList<string> RolesFor(string email)
    {
        var normalized = email.Trim().ToLowerInvariant();
        return normalized switch
        {
            LabEmail => new[] { "LabManager", "LabOperator" },
            HubEmail => new[] { "HubQC", "HubFulfillment" },
            OpsEmail => new[] { "OpsManager", "Admin" },
            _ => new[] { "Customer" },
        };
    }
}