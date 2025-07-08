namespace WebApplication1.SharedContracts.Commands
{
    public record SubmitOrder(Guid OrderId, Guid CustomerId, decimal TotalAmount, bool IsDigitalOnly);
    public record ProcessPayment(Guid OrderId, decimal Amount);
    public record ReserveInventory(Guid OrderId, List<Guid> ItemIds);
    public record ConfirmOrder(Guid OrderId);
    public record RefundPayment(Guid OrderId, decimal Amount);
    public record ReleaseInventory(Guid OrderId, List<Guid> ItemIds);
    public record InitiateShipping(Guid OrderId); // New Command
    public record PerformCreditCheck(Guid OrderId, Guid CustomerId, decimal TotalAmount); // New Command
}
