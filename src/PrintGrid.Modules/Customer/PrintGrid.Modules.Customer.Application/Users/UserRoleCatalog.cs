namespace PrintGrid.Modules.Customer.Application.Users;

/// <summary>
/// Single source of truth for account roles exposed by the admin UI/API.
/// BR-ACCESS-005: a staff account carries exactly one role; Customer is the
/// only self-service role and cannot be combined with an operational role.
/// </summary>
public static class UserRoleCatalog
{
    public static readonly IReadOnlyList<RoleDefinition> All =
    [
        new("Customer", "Khách hàng", ["models.read", "models.write", "orders.read", "orders.write"]),
        new("LabManager", "Quản lý xưởng", ["jobs.read", "jobs.execute", "inventory.manage"]),
        new("LabOperator", "Vận hành máy", ["jobs.read", "jobs.execute"]),
        new("HubQC", "Kiểm định hub", ["jobs.read", "quality.inspect", "shipments.read"]),
        new("HubFulfillment", "Đóng gói & giao hàng", ["shipments.read", "shipments.manage"]),
        new("OpsManager", "Điều phối mạng lưới", ["scheduling.manage", "operations.manage", "quotes.review", "qc-proofs.review"]),
        new("Admin", "Quản trị hệ thống", ["users.manage", "audit.read", "system.manage"]),
    ];

    public static bool IsKnown(string? role) =>
        !string.IsNullOrWhiteSpace(role) && All.Any(x => string.Equals(x.Code, role.Trim(), StringComparison.OrdinalIgnoreCase));

    public static string Canonicalize(string role) =>
        All.First(x => string.Equals(x.Code, role.Trim(), StringComparison.OrdinalIgnoreCase)).Code;
}

public sealed record RoleDefinition(string Code, string Label, IReadOnlyList<string> Permissions);
