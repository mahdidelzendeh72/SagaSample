// Program.cs (for .NET 6+ Minimal API)


using MassTransit;
using Microsoft.EntityFrameworkCore;
//using Serilog;
using WebApplication1.Consumer;
using WebApplication1.Database;
using WebApplication1.Interfaces;
using WebApplication1.Saga;
using WebApplication1.SharedContracts.Commands;

var builder = WebApplication.CreateBuilder(args);

// Configure logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();
builder.Services.AddScoped<ICustomInterface, CustomImplementation>();
// Configure DbContext for Saga Persistence
builder.Services.AddDbContext<OrderSagaDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("SagaDbConnection"));
});

// Configure MassTransit
builder.Services.AddMassTransit(x =>
{
    // Register the Saga State Machine and its repository
    x.AddSagaStateMachine<OrderStateMachine, OrderState>()
        .EntityFrameworkRepository(r =>
        {

            r.ConcurrencyMode = ConcurrencyMode.Optimistic; // Or Pessimistic
            r.ExistingDbContext<OrderSagaDbContext>();
            r.UseSqlServer();
        });

    // Register consumers
    x.AddConsumer<ProcessPaymentConsumer>();
    x.AddConsumer<RefundPaymentConsumer>();
    x.AddConsumer<ReserveInventoryConsumer>();
    x.AddConsumer<ReleaseInventoryConsumer>();
    x.AddConsumer<PerformCreditCheckConsumer>();

    // Configure RabbitMQ as the message broker
    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host("amqp://localhost:5672", h =>
        {
            h.Username("your_username");
            h.Password("your_password");
        });

        // Configure receive endpoint for the saga
        // This is where the saga listens for events
        cfg.ReceiveEndpoint("order-saga-queue", e =>
        {
            e.ConfigureSaga<OrderState>(context);
        });
        cfg.ReceiveEndpoint("credit_check_service_queue", e =>
        {
            e.ConfigureConsumer<PerformCreditCheckConsumer>(context);
        });
        // Configure receive endpoints for the consumers
        cfg.ReceiveEndpoint("payment_queue", e =>
        {
            e.ConfigureConsumer<ProcessPaymentConsumer>(context);
        });
        cfg.ReceiveEndpoint("payment_refund_queue", e =>
        {
            e.ConfigureConsumer<RefundPaymentConsumer>(context);
        });
        cfg.ReceiveEndpoint("inventory_queue", e =>
        {
            e.ConfigureConsumer<ReserveInventoryConsumer>(context);
        });
        cfg.ReceiveEndpoint("inventory_release_queue", e =>
        {
            e.ConfigureConsumer<ReleaseInventoryConsumer>(context);
        });

        // Optional: Configure outbox for reliable message publishing
        cfg.UseInMemoryOutbox(context); // Or use UseEntityFrameworkOutbox for durable outbox

    });
    x.AddEntityFrameworkOutbox<OrderSagaDbContext>((config) =>
    {
        config.UseSqlServer();
        config.UseBusOutbox();
        config.QueryDelay = TimeSpan.FromSeconds(10);
        config.DuplicateDetectionWindow = TimeSpan.FromSeconds(60);
    });

});

builder.Services.AddEndpointsApiExplorer();
//builder.Services.ConfigureSwagger();
builder.Services.AddSwaggerGen();
var app = builder.Build();

// Apply migrations for the saga database
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<OrderSagaDbContext>();
    dbContext.Database.Migrate();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Simple API endpoint to initiate an order

app.MapPost("/submit-order", async (IBus messageBus, Guid customerId, decimal totalAmount) =>
{
    var orderId = NewId.NextGuid(); // Use MassTransit's NewId for better GUIDs

    await messageBus.Publish(new SubmitOrder(orderId, customerId, totalAmount, false));

    return Results.Accepted($"Order {orderId} submitted.");
})
.WithName("SubmitOrder");


app.Run();