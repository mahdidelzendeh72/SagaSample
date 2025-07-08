namespace WebApplication1.SharedContracts.Events
{
    public record OrderSubmitted(Guid OrderId, Guid CustomerId, decimal TotalAmount);
    public record PaymentProcessed(Guid OrderId, string TransactionId);
    public record PaymentFailed(Guid OrderId, string Reason);
    public record InventoryReserved(Guid OrderId);
    public record InventoryReservationFailed(Guid OrderId, string Reason);
    public record OrderConfirmed(Guid OrderId);
    public record OrderFailed(Guid OrderId, string Reason);
    public record PaymentRefunded(Guid OrderId);
    public record InventoryReleased(Guid OrderId);
    public record ShippingCompleted(Guid OrderId); // New Event
    public record DigitalOrderDelivered(Guid OrderId); // New Event
    public record CreditCheckResult(Guid OrderId, bool IsApproved, string Rsason = null); // New Event

    // New internal event for the timeout
    public record CreditCheckTimeout(Guid OrderId);


}
