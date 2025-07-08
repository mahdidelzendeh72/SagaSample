using MassTransit;

namespace WebApplication1.Saga;
// OrderProcessingSaga/OrderState.cs

public class OrderState : SagaStateMachineInstance
{
    public Guid CorrelationId { get; set; } // Required by SagaStateMachineInstance
    public string CurrentState { get; set; } // Stores the current state as a string

    // Business data for the saga
    public Guid CustomerId { get; set; }
    public decimal TotalAmount { get; set; }
    public string? PaymentTransactionId { get; set; }
    public DateTime OrderDate { get; set; }
    public string? FailureReason { get; set; }
    public bool IsDigitalOnly { get; set; }

    // Required for optimistic concurrency with some repositories (e.g., Redis)
    //public int Version { get; set; }

    // Required for scheduling (timeout)

}
