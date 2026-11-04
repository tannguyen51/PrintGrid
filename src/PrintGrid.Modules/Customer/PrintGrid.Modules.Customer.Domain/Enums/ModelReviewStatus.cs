namespace PrintGrid.Modules.Customer.Domain.Enums;

/// <summary>
/// Content-moderation state of a customer model. A model may only be quoted and ordered
/// once an Operations Manager approved it (pending models are blocked before production).
/// Existing rows were grandfathered to <see cref="Approved"/> by migration.
/// </summary>
public enum ModelReviewStatus
{
    Pending = 0,
    Approved = 1,
    Rejected = 2
}
