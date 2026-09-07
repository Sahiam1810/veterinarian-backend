using Application.Owners.Abstractions;
using Application.Owners.Enums;
using Infrastructure.Owners.Configuration;
using Microsoft.Extensions.Options;

namespace Infrastructure.Owners;

// Bot siempre ConsumeProof (Etapa 3). Telegram: equivalencia OTP de sesión (ADR 4.3).
// Staff solo si RegisterOwner:RequireContactProofs=true.
public sealed class ConfiguredRegisterOwnerSettings(
    IOptions<RegisterOwnerOptions> options) : IRegisterOwnerSettings
{
    private readonly RegisterOwnerOptions _options = options.Value;

    public bool RequireContactProofs => _options.RequireContactProofs;

    public bool RequiresContactProof(RegisterOwnerChannel channel) =>
        channel switch
        {
            RegisterOwnerChannel.Bot => true,
            RegisterOwnerChannel.Telegram => false,
            _ => _options.RequireContactProofs
        };
}
