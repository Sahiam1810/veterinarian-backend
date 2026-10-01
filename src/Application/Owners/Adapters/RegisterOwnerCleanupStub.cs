using Application.Owners.Abstractions;

namespace Application.Owners.Adapters;

// Kickoff: 4.5 implementa el barrido. No toca el flujo Telegram vigente.
public sealed class RegisterOwnerCleanupStub : IRegisterOwnerCleanup
{
    public Task<int> SweepExpiredSessionsAsync(CancellationToken cancellationToken) =>
        Task.FromResult(0);
}
