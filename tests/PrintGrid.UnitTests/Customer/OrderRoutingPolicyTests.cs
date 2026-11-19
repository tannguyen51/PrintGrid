using PrintGrid.SharedKernel.Interfaces;

namespace PrintGrid.UnitTests.Customer;

public class OrderRoutingPolicyTests
{
    [Theory]
    [InlineData(1, false)]
    [InlineData(4, false)]
    [InlineData(5, true)]
    [InlineData(15, true)]
    public void Orders_from_five_items_require_ops_review(int quantity, bool requiresOpsReview)
    {
        OrderRoutingPolicy.RequiresManualDispatch(quantity).Should().Be(requiresOpsReview);
    }
}
