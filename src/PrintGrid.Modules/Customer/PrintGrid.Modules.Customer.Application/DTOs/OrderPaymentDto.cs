namespace PrintGrid.Modules.Customer.Application.DTOs;

/// <summary>FR-CUST-012: deposit/settlement snapshot for one order.</summary>
public record OrderPaymentDto(
    decimal TotalAmount,
    decimal DepositAmount,
    decimal PaidAmount,
    decimal Remaining,
    bool IsDeposited,
    bool IsSettled,
    string Currency);
