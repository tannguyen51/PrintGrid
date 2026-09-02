using Hangfire.Annotations;
using Hangfire.Dashboard;
using PrintGrid.Api.Authorization;

namespace PrintGrid.Api.Bootstrap;

public class HangfireDashboardAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize([NotNull] DashboardContext context)
    {
        var user = context.GetHttpContext().User;
        return user.Identity?.IsAuthenticated == true && user.IsInRole(Roles.Admin);
    }
}
