using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MassLab.Common.Outbox.Extensions;
using MassLab.Common.Outbox.Entities;

namespace MassLab.Common.Outbox.Tests;

public class OutboxTests
{
    [Fact]
    public void Outbox_message_defaults_to_pending_state()
    {
        var message = new OutboxMessage { Type = "Event", Payload = "{}" };

        message.Id.Should().NotBeEmpty();
        message.ProcessedOn.Should().BeNull();
        message.Attempts.Should().Be(0);
    }

    [Fact]
    public void Entity_configuration_can_be_applied_to_model()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using var db = new TestDbContext(options);

        db.Model.FindEntityType(typeof(OutboxMessage))!.GetTableName().Should().Be("OutboxMessages");
    }

    [Fact]
    public void AddOutbox_rejects_invalid_options()
    {
        var services = new ServiceCollection();

        var act = () => services.AddOutbox<TestDbContext>(options => options.BatchSize = 0);

        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("BatchSize");
    }

    private sealed class TestDbContext(DbContextOptions<TestDbContext> options) : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
            => modelBuilder.ApplyConfiguration(new OutboxMessageConfiguration());
    }
}
