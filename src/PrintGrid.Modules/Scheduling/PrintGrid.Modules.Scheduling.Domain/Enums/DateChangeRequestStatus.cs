namespace PrintGrid.Modules.Scheduling.Domain.Enums;

public enum DateChangeRequestStatus
{
    /// <summary>Stated to the customer, waiting for their answer (BR-SCHED-008).</summary>
    Proposed,
    Approved,
    Rejected,

    /// <summary>The customer never answered before the deadline (UC-012 extension).</summary>
    Expired
}
