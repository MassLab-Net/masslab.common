using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Victor.Common.Outbox.Entities;

/// <summary>
/// Persistent record of an integration / domain event awaiting publication.
/// Written inside the originating transaction; dispatched asynchronously
/// by <c>OutboxBackgroundService</c>.
/// </summary>
public class OutboxMessage
{
    /// <summary>Primary key.</summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Assembly-qualified type name of the event payload.</summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>JSON-serialized event payload.</summary>
    public string Payload { get; set; } = string.Empty;

    /// <summary>UTC instant the event was raised.</summary>
    public DateTime OccurredOn { get; set; } = DateTime.UtcNow;

    /// <summary>UTC instant the event was successfully dispatched (null when pending).</summary>
    public DateTime? ProcessedOn { get; set; }

    /// <summary>Number of dispatch attempts.</summary>
    public int Attempts { get; set; }

    /// <summary>Earliest UTC instant for the next retry attempt.</summary>
    public DateTime? NextAttemptAt { get; set; }

    /// <summary>Last error message (truncated).</summary>
    public string? Error { get; set; }
}

/// <summary>
/// EFCore configuration for <see cref="OutboxMessage"/>. Apply via
/// <c>modelBuilder.ApplyConfiguration(new OutboxMessageConfiguration())</c>
/// inside your <c>DbContext.OnModelCreating</c>.
/// </summary>
public class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("OutboxMessages");
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Type).IsRequired().HasMaxLength(512);
        builder.Property(m => m.Payload).IsRequired();
        builder.Property(m => m.OccurredOn).IsRequired();
        builder.Property(m => m.Error).HasMaxLength(2000);
        builder.HasIndex(m => m.ProcessedOn);
        builder.HasIndex(m => m.OccurredOn);
    }
}
