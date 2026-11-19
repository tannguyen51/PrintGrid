namespace PrintGrid.SharedKernel.Interfaces;

/// <summary>BR-ORDER-ROUTE-001: quantity routing is independent of quote price approval.</summary>
public static class OrderRoutingPolicy
{
    public const int ManualDispatchFromQuantity = 5;
    public static bool RequiresManualDispatch(int totalQuantity) => totalQuantity >= ManualDispatchFromQuantity;
}
