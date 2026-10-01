namespace Application.Owners.Abstractions;

// Barrido de sesiones de contacto vencidas (4.x). Kickoff: no-op hasta el adaptador de cleanup.
public interface IRegisterOwnerCleanup
{
    Task<int> SweepExpiredSessionsAsync(CancellationToken cancellationToken);
}
