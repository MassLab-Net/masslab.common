using MassLab.Common.Email.Abstractions;
using MassLab.Common.Email.Models;

namespace MassLab.Common.Email.Services;

public sealed class EmailSenderFactory(IEnumerable<IEmailProviderSenderFactory> providers) : IEmailSenderFactory
{
    private readonly IReadOnlyDictionary<EmailProviderKind, IEmailProviderSenderFactory> _providers = providers.ToDictionary(x => x.Provider);
    public IConfiguredEmailSender Create(EmailProviderConfiguration configuration)
        => _providers.TryGetValue(configuration.Provider, out var provider)
            ? provider.Create(configuration)
            : throw new InvalidOperationException($"Email provider '{configuration.Provider}' is not registered.");
}
