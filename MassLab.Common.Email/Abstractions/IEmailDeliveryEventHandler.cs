using MassLab.Common.Email.Models;

namespace MassLab.Common.Email.Abstractions;

/// <summary>Handles a verified provider lifecycle event. Implementations must be idempotent by EventId.</summary>
public interface IEmailDeliveryEventHandler
{
    Task HandleAsync(EmailLifecycleEvent emailEvent, CancellationToken cancellationToken = default);
}
