namespace PrintGrid.Api.Authorization;

public static class Roles
{
    public const string Customer = "Customer";
    public const string LabManager = "LabManager";
    public const string LabOperator = "LabOperator";
    public const string HubQc = "HubQC";
    public const string HubFulfillment = "HubFulfillment";
    public const string OpsManager = "OpsManager";
    public const string Admin = "Admin";
}

public static class Policies
{
    public const string RequireCustomer = "RequireCustomer";
    public const string RequireLab = "RequireLab";
    public const string RequireHub = "RequireHub";
    public const string RequireOps = "RequireOps";
    public const string RequireAdmin = "RequireAdmin";
}
