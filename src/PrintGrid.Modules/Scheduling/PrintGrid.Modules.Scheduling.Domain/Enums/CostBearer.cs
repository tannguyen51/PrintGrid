namespace PrintGrid.Modules.Scheduling.Domain.Enums;

/// <summary>
/// Who bears the cost of a (re)print. A reprint caused by a fault is charged to the
/// at-fault party (BR-RESCHED-003 / 13-Acceptance-Criteria.md:330 "Cost=Charged to lab"),
/// never to the customer who already paid for the original job.
/// </summary>
public enum CostBearer
{
    Customer,
    Lab
}
