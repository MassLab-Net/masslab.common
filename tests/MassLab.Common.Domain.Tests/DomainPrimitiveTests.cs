using System.Linq.Expressions;
using MassLab.Common.Domain.Entities;
using MassLab.Common.Domain.Events;
using MassLab.Common.Domain.Specifications;

namespace MassLab.Common.Domain.Tests;

public class DomainPrimitiveTests
{
    [Fact]
    public void Entities_with_same_id_and_type_are_equal()
    {
        var id = Guid.NewGuid();

        var left = new TestEntity(id);
        var right = new TestEntity(id);

        left.Should().Be(right);
        left.GetHashCode().Should().Be(right.GetHashCode());
    }

    [Fact]
    public void Entities_with_same_id_but_different_type_are_not_equal()
    {
        var id = Guid.NewGuid();

        var left = new TestEntity(id);
        var right = new OtherEntity(id);

        left.Should().NotBe(right);
        left.GetHashCode().Should().NotBe(right.GetHashCode());
    }

    [Fact]
    public void Aggregate_root_tracks_and_clears_domain_events()
    {
        var aggregate = new TestAggregate(Guid.NewGuid());
        var domainEvent = new TestDomainEvent(DateTimeOffset.UtcNow);

        aggregate.Record(domainEvent);

        aggregate.DomainEvents.Should().ContainSingle().Which.Should().Be(domainEvent);

        aggregate.ClearDomainEvents();

        aggregate.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Value_objects_compare_by_components()
    {
        var left = new Money("USD", 12.30m);
        var right = new Money("USD", 12.30m);
        var different = new Money("EUR", 12.30m);

        left.Should().Be(right);
        left.Should().NotBe(different);
    }

    [Theory]
    [InlineData(-1, 10)]
    [InlineData(0, 0)]
    public void Specification_rejects_invalid_paging(int skip, int take)
    {
        var act = () => new InvalidPagingSpec(skip, take);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    private sealed class TestEntity : Entity
    {
        public TestEntity(Guid id) : base(id) { }
    }

    private sealed class OtherEntity : Entity
    {
        public OtherEntity(Guid id) : base(id) { }
    }

    private sealed class TestAggregate : AggregateRoot
    {
        public TestAggregate(Guid id) : base(id) { }

        public void Record(IDomainEvent domainEvent) => RaiseDomainEvent(domainEvent);
    }

    private sealed record TestDomainEvent(DateTimeOffset OccurredOn) : IDomainEvent;

    private sealed class Money : ValueObject
    {
        private readonly string _currency;
        private readonly decimal _amount;

        public Money(string currency, decimal amount)
        {
            _currency = currency;
            _amount = amount;
        }

        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return _currency;
            yield return _amount;
        }
    }

    private sealed class InvalidPagingSpec : Specification<TestEntity>
    {
        public InvalidPagingSpec(int skip, int take) : base(True())
        {
            ApplyPaging(skip, take);
        }

        private static Expression<Func<TestEntity, bool>> True() => _ => true;
    }
}
