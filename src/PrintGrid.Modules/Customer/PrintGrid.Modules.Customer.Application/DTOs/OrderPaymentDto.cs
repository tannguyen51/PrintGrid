namespace PrintGrid.Modules.Customer.Application.DTOs;

/// <summary>FR-CUST-012: deposit/settlement snapshot for one order.</summary>
public record OrderPaymentDto(
    bool IsDeposited, bool IsSettled, decimal PaidAmount,
    decimal TotalAmount, decimal DepositAmount, decimal Remaining, decimal AmountDue,
    string Currency, string PaymentCode);
