namespace WebApplication1.Consumer;

// PaymentService/ProcessPaymentConsumer.cs
using MassTransit;
using SharedContracts.Commands;
using SharedContracts.Events;
using System;
using System.Threading.Tasks;

public class ProcessPaymentConsumer : IConsumer<ProcessPayment>
{
    private readonly ILogger<ProcessPaymentConsumer> _logger;

    public ProcessPaymentConsumer(ILogger<ProcessPaymentConsumer> logger)
    {
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<ProcessPayment> context)
    {
        _logger.LogInformation($"Payment Service: Processing payment for OrderId: {context.Message.OrderId}, Amount: {context.Message.Amount}");

        // Simulate payment processing logic
        bool paymentSuccessful = true; // In a real scenario, this would depend on external payment gateway
        if (context.Message.Amount < 1000) // Simulate a failure for large amounts
        {
            paymentSuccessful = false;
        }

        if (paymentSuccessful)
        {
            await context.Publish(new PaymentProcessed(context.Message.OrderId, $"TXN-{Guid.NewGuid()}"));
            _logger.LogInformation($"Payment Service: Payment processed successfully for OrderId: {context.Message.OrderId}");
        }
        else
        {
            await context.Publish(new PaymentFailed(context.Message.OrderId, "Insufficient funds or large amount policy violated."));
            _logger.LogWarning($"Payment Service: Payment failed for OrderId: {context.Message.OrderId}");
        }
    }
}

// PaymentService/RefundPaymentConsumer.cs (for compensation)
public class RefundPaymentConsumer : IConsumer<RefundPayment>
{
    private readonly ILogger<RefundPaymentConsumer> _logger;

    public RefundPaymentConsumer(ILogger<RefundPaymentConsumer> logger)
    {
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<RefundPayment> context)
    {
        _logger.LogInformation($"Payment Service: Refunding payment for OrderId: {context.Message.OrderId}, Amount: {context.Message.Amount}");
        // Simulate refund logic
        await context.Publish(new PaymentRefunded(context.Message.OrderId));
        _logger.LogInformation($"Payment Service: Payment refunded for OrderId: {context.Message.OrderId}");
    }
}

