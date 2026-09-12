using Application.Owners.Enums;

namespace Application.Owners.Abstractions;

// Flag documentado: staff respeta RequireContactProofs; bot/Telegram siempre exigen proof.
public interface IRegisterOwnerSettings
{
    bool RequireContactProofs { get; }

    bool RequiresContactProof(RegisterOwnerChannel channel);
}
