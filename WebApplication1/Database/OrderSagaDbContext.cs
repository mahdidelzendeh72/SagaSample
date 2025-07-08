namespace WebApplication1.Database;

// OrderProcessingSaga/OrderSagaDbContext.cs
using MassTransit;
using MassTransit.EntityFrameworkCoreIntegration;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using WebApplication1.Saga;


public class OrderSagaDbContext : SagaDbContext
{
    public DbSet<OrderState> SagaData { get; set; }
    public OrderSagaDbContext(DbContextOptions<OrderSagaDbContext> options) : base(options)
    {
    }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.AddInboxStateEntity();
        modelBuilder.AddOutboxMessageEntity();
        modelBuilder.AddOutboxStateEntity();
    }
    protected override IEnumerable<ISagaClassMap> Configurations
    {
        get { yield return new OrderStateMap(); }
    }
}

// OrderProcessingSaga/OrderStateMap.cs
public class OrderStateMap : SagaClassMap<OrderState>
{
    protected override void Configure(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<OrderState> entity, ModelBuilder model)
    {
        entity.HasKey(b => b.CorrelationId);
        entity.Property(x => x.CurrentState).HasMaxLength(64);
        entity.Property(x => x.CustomerId);
        entity.Property(x => x.TotalAmount).HasColumnType("decimal(18,2)");
        entity.Property(x => x.PaymentTransactionId).HasMaxLength(256);
        entity.Property(x => x.OrderDate);
        entity.Property(x => x.FailureReason).HasMaxLength(500);

        // Configure for optimistic concurrency if using Redis or EF Core with RowVersion
        // For EF Core, you'd typically add a RowVersion property to OrderState and configure it here.
        // For simplicity in this example, we're relying on MassTransit's default EF Core optimistic concurrency
        // which often uses the default EF Core behavior for concurrency tokens.
        // If `Version` is explicitly used for optimistic concurrency, ensure it's configured as a concurrency token.
        // entity.Property(x => x.Version).IsRowVersion(); // Example for RowVersion if used
    }
}

