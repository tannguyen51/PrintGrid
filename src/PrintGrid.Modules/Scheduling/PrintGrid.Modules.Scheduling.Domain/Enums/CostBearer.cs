namespace PrintGrid.Modules.Scheduling.Domain.Enums;

/// <summary>
/// Who bears the cost of a (re)print. A reprint caused by a fault is charged to the at-fault
/// party (BR-RESCHED-003 / 13-Acceptance-Criteria.md:330), never to the customer who already
/// paid for the original job: a lab fault is charged to the lab, a hub fault is borne by the
/// platform itself, and a customer-caused reprint is a paid reprint for the customer.
/// </summary>
public enum CostBearer
{
    Customer,
    Lab,

    /// <summary>The platform absorbs it — the workshop network did nothing wrong (hub fault).</summary>
    System
}
