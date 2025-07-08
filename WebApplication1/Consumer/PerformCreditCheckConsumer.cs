namespace WebApplication1.Consumer;

// PaymentService/ProcessPaymentConsumer.cs
using MassTransit;
using SharedContracts.Commands;
using SharedContracts.Events;
using System;
using System.Threading.Tasks;

public class PerformCreditCheckConsumer : IConsumer<PerformCreditCheck>
{
    public async Task Consume(ConsumeContext<PerformCreditCheck> context)
    {
        var orderId = context.Message.OrderId;
        var customerId = context.Message.CustomerId;
        var totalAmount = context.Message.TotalAmount;

        Console.WriteLine($"CreditCheckConsumer: Received request for OrderId: {orderId}, CustomerId: {customerId}, Amount: {totalAmount}");

        // --- Simulate Credit Check Logic ---
        // In a real application, you would call an external credit agency API here.
        // For demonstration, let's simulate a delay and a conditional approval/decline.

        await Task.Delay(TimeSpan.FromSeconds(2)); // Simulate external API call delay

        bool isApproved = true;
        string? reason = null;

        // Example: Decline if total amount is too high, or based on customer ID
        if (totalAmount > 5000m)
        {
            isApproved = false;
            reason = "Amount exceeds credit limit.";
        }
        else if (customerId.ToString().EndsWith("0")) // Just an arbitrary condition for decline
        {
            isApproved = false;
            reason = "Customer flagged for review.";
        }


        // --- Send the Response back to the Saga ---
        if (isApproved)
        {
            Console.WriteLine($"CreditCheckConsumer: Credit approved for OrderId: {orderId}");
            // Use RespondAsync to send the CreditCheckResult back to the saga
            await context.RespondAsync(new CreditCheckResult(orderId, true));
        }
        else
        {
            Console.WriteLine($"CreditCheckConsumer: Credit declined for OrderId: {orderId}. Reason: {reason}");
            // For a business-level decline, you still use RespondAsync with the result.
            // The saga's When(ProcessCredit.Completed).If(!context.Message.IsApproved) will handle this.
            await context.RespondAsync(new CreditCheckResult(orderId, false, reason));
        }
    }
}

