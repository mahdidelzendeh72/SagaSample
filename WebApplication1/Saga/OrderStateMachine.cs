namespace WebApplication1.Saga;

using MassTransit;
// OrderProcessingSaga/OrderStateMachine.cs
using SharedContracts.Commands;
using SharedContracts.Events;
using System;
using WebApplication1.Interfaces;

public class OrderStateMachine : MassTransitStateMachine<OrderState>
{
    public State OrderSubmitted { get; private set; }
    public State CreditChecking { get; private set; }
    public State PaymentProcessing { get; private set; }
    public State InventoryReservation { get; private set; }
    public State ShippingProcessing { get; private set; }
    public State OrderCompleted { get; private set; }
    public State OrderFailed { get; private set; }

    public Event<SubmitOrder> SubmitOrder { get; private set; }
    public Event<PaymentProcessed> PaymentProcessed { get; private set; }
    public Event<PaymentFailed> PaymentFailed { get; private set; }
    public Event<InventoryReserved> InventoryReserved { get; private set; }
    public Event<InventoryReservationFailed> InventoryReservationFailed { get; private set; }
    public Event<ShippingCompleted> ShippingCompleted { get; private set; } // New Event


    //only define request not event
    public Request<OrderState, PerformCreditCheck, CreditCheckResult> ProcessCredit { get; set; }


    private IServiceScopeFactory serviceScopeFactory;
    public OrderStateMachine(IServiceScopeFactory serviceScopeFactory)
    {
        serviceScopeFactory = serviceScopeFactory;
        InstanceState(x => x.CurrentState); // Binds the CurrentState property to the saga's state
        // --- New: Configure the Request property ---
        Request(() => ProcessCredit, r =>
        {
            r.ServiceAddress = new Uri("queue:credit_check_service_queue"); // The target service endpoint
            r.Timeout = TimeSpan.FromSeconds(30); // How long to wait for a response
        });
        // Define states and events
        Event(() => SubmitOrder, x => x.CorrelateById(context => context.Message.OrderId));
        Event(() => PaymentProcessed, x => x.CorrelateById(context => context.Message.OrderId));
        Event(() => PaymentFailed, x => x.CorrelateById(context => context.Message.OrderId));
        Event(() => InventoryReserved, x => x.CorrelateById(context => context.Message.OrderId));
        Event(() => InventoryReservationFailed, x => x.CorrelateById(context => context.Message.OrderId));
        Event(() => ShippingCompleted, x => x.CorrelateById(context => context.Message.OrderId)); // Correlate ShippingCompleted


        // Initial state: When SubmitOrder is received, a new saga instance is created.
        Initially(
            When(SubmitOrder)
                .Then(async context =>
                {
                    using (var scope = serviceScopeFactory.CreateScope())
                    {
                        var customInterface = scope.ServiceProvider.GetService<ICustomInterface>();
                        await customInterface.DoSomethings("order submitted");

                    }
                    context.Saga.CustomerId = context.Message.CustomerId;
                    context.Saga.TotalAmount = context.Message.TotalAmount;
                    context.Saga.OrderDate = DateTime.UtcNow;
                    context.Saga.IsDigitalOnly = context.Message.IsDigitalOnly;
                    Console.WriteLine($"Saga: Order {context.Saga.CorrelationId} submitted. Initiating payment.");
                })
                .Request(ProcessCredit, context => new PerformCreditCheck( // Send the request
                    context.Saga.CorrelationId,
                    context.Saga.CustomerId,
                    context.Saga.TotalAmount
                ))
                .If(context => context.Saga.TotalAmount > 1000m,
                    binder => binder
                        .Then(context =>
                        {
                            Console.WriteLine($"Saga: High-value order {context.Saga.CorrelationId}. Sending manager notification.");
                            // Example: Publish an event for manager notification
                            // context.Publish(new ManagerNotification(context.Saga.CorrelationId, "High-value order processed."));
                        })
                )
                .TransitionTo(CreditChecking)
        );

        // New State: During CreditChecking
        During(CreditChecking,
           // --- Handle the responses for ProcessCredit request ---
           When(ProcessCredit.Completed) // Only ONE When(ProcessCredit.Completed) block
              .IfElse(context => context.Message.IsApproved, // Condition: Is credit approved?
                                                             // THEN branch: If IsApproved is true (Credit Approved)
                thenBinder => thenBinder
                .Then(context =>
                {
                    Console.WriteLine($"Saga: Credit check approved for order {context.Saga.CorrelationId}.");
                }).Publish(context => new ProcessPayment(context.Saga.CorrelationId, context.Saga.TotalAmount))
                .TransitionTo(PaymentProcessing), // Proceed to next state

               // ELSE branch: If IsApproved is false (Credit Declined)
               elseBinder => elseBinder
                .Then(context =>
                {
                    context.Saga.FailureReason = "Credit check declined by agency";
                    Console.WriteLine($"Saga: Credit check declined for order {context.Saga.CorrelationId}. Moving to failed state.");
                })
                .Publish(context => new OrderFailed(context.Saga.CorrelationId, context.Saga.FailureReason))
                .TransitionTo(OrderFailed) // Move to failed state
        ),

            When(ProcessCredit.Faulted) // When an unhandled exception occurs in the credit check service
                .Then(context =>
                {
                    context.Saga.FailureReason = $"Credit check service faulted: {context.Message.Message}";
                    Console.WriteLine($"Saga: Credit check failed for order {context.Saga.CorrelationId}. Reason: {context.Saga.FailureReason}. Moving to failed state.");
                })
                .Publish(context => new OrderFailed(context.Saga.CorrelationId, context.Saga.FailureReason))
                .TransitionTo(OrderFailed),

            When(ProcessCredit.TimeoutExpired) // When the timeout configured on the Request property expires
                .Then(context =>
                {
                    context.Saga.FailureReason = "Credit check timed out";
                    Console.WriteLine($"Saga: Credit check timed out for order {context.Saga.CorrelationId}. Moving to failed state.");
                })
                .Publish(context => new OrderFailed(context.Saga.CorrelationId, context.Saga.FailureReason))
                .TransitionTo(OrderFailed)


        );
        // During PaymentProcessing state
        During(PaymentProcessing,
            When(PaymentProcessed)
                .Then(context =>
                {
                    context.Saga.PaymentTransactionId = context.Message.TransactionId;
                    Console.WriteLine($"Saga: Payment processed for order {context.Saga.CorrelationId}. Reserving inventory.");
                })
                .Retry(r => r.Interval(3, TimeSpan.FromSeconds(5)),
                    // The actions to retry
                    retryBinder => retryBinder
                        .Publish(context => new ReserveInventory(context.Saga.CorrelationId, new List<Guid> { Guid.NewGuid() }))
                )
                //.Send(new Uri("queue:inventory_queue"), context => new ReserveInventory(context.Saga.CorrelationId, new List<Guid> { Guid.NewGuid() })) // Placeholder for items
                .TransitionTo(InventoryReservation),

            When(PaymentFailed)
                .Then(context =>
                {
                    context.Saga.FailureReason = context.Message.Reason;
                    Console.WriteLine($"Saga: Payment failed for order {context.Saga.CorrelationId}. Reason: {context.Message.Reason}. Moving to failed state.");
                })
                .Publish(context => new OrderFailed(context.Saga.CorrelationId, context.Saga.FailureReason ?? "Payment failed"))
                .TransitionTo(OrderFailed)
        );

        // During InventoryReservation state
        During(InventoryReservation,
            When(InventoryReserved)
                .Then(context =>
                {
                    Console.WriteLine($"Saga: Inventory reserved for order {context.Saga.CorrelationId}. Confirming order.");
                })
                 .IfElse(context => context.Saga.IsDigitalOnly, // Condition: Is it a digital-only order?
                                                                // If true (digital only):
                    thenBinder => thenBinder
                        .Then(context =>
                        {
                            Console.WriteLine($"Saga: Digital-only order {context.Saga.CorrelationId}. Completing order and delivering digitally.");
                        })
                        .Publish(context => new DigitalOrderDelivered(context.Saga.CorrelationId)) // Publish digital delivery event
                        .Publish(context => new OrderConfirmed(context.Saga.CorrelationId))
                        .TransitionTo(OrderCompleted)
                        .Finalize(),

                    // Else (physical or mixed items):
                    elseBinder => elseBinder
                        .Then(context =>
                        {
                            Console.WriteLine($"Saga: Physical/mixed order {context.Saga.CorrelationId}. Initiating shipping process.");
                        })
                        .Publish(context => new InitiateShipping(context.Saga.CorrelationId)) // Publish shipping initiation event
                        .TransitionTo(ShippingProcessing) // Transition to the new ShippingProcessing state
                ),


            When(InventoryReservationFailed)
                .Then(context =>
                {
                    context.Saga.FailureReason = context.Message.Reason;
                    Console.WriteLine($"Saga: Inventory reservation failed for order {context.Saga.CorrelationId}. Reason: {context.Message.Reason}. Refunding payment.");
                })
                .Publish(context => new OrderFailed(context.Saga.CorrelationId, context.Saga.FailureReason ?? "Inventory reservation failed"))
                .Send(new Uri("queue:payment_refund_queue"), context => new RefundPayment(context.Saga.CorrelationId, context.Saga.TotalAmount))
                .TransitionTo(OrderFailed)


        );
        During(ShippingProcessing,
           When(ShippingCompleted)
               .Then(context =>
               {
                   Console.WriteLine($"Saga: Shipping completed for order {context.Saga.CorrelationId}. Confirming order.");
               })
               .Publish(context => new OrderConfirmed(context.Saga.CorrelationId))
               .TransitionTo(OrderCompleted)
               .Finalize()
       );
        // When the saga enters the Final state, optionally remove it from the repository
        SetCompletedWhenFinalized();



    }
}

