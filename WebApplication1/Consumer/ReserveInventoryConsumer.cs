namespace WebApplication1.Consumer;

// InventoryService/ReserveInventoryConsumer.cs
using MassTransit;
using SharedContracts.Commands;
using SharedContracts.Events;


public class ReserveInventoryConsumer : IConsumer<ReserveInventory>
{
    private readonly ILogger<ReserveInventoryConsumer> _logger;

    public ReserveInventoryConsumer(ILogger<ReserveInventoryConsumer> logger)
    {
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<ReserveInventory> context)
    {
        _logger.LogInformation($"Inventory Service: Reserving inventory for OrderId: {context.Message.OrderId}, Items: {string.Join(", ", context.Message.ItemIds)}");

        // Simulate inventory reservation logic
        bool reservationSuccessful = true;
        if (context.Message.OrderId.ToString().StartsWith("0000")) // Simulate a failure for a specific order ID
        {
            reservationSuccessful = false;
        }

        if (reservationSuccessful)
        {
            await context.Publish(new InventoryReserved(context.Message.OrderId));
            _logger.LogInformation($"Inventory Service: Inventory reserved successfully for OrderId: {context.Message.OrderId}");
        }
        else
        {
            await context.Publish(new InventoryReservationFailed(context.Message.OrderId, "Out of stock for some items."));
            _logger.LogWarning($"Inventory Service: Inventory reservation failed for OrderId: {context.Message.OrderId}");
        }
    }
}

// InventoryService/ReleaseInventoryConsumer.cs (for compensation)
public class ReleaseInventoryConsumer : IConsumer<ReleaseInventory>
{
    private readonly ILogger<ReleaseInventoryConsumer> _logger;

    public ReleaseInventoryConsumer(ILogger<ReleaseInventoryConsumer> logger)
    {
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<ReleaseInventory> context)
    {
        _logger.LogInformation($"Inventory Service: Releasing inventory for OrderId: {context.Message.OrderId}, Items: {string.Join(", ", context.Message.ItemIds)}");
        // Simulate inventory release logic
        await context.Publish(new InventoryReleased(context.Message.OrderId));
        _logger.LogInformation($"Inventory Service: Inventory released for OrderId: {context.Message.OrderId}");
    }
}

