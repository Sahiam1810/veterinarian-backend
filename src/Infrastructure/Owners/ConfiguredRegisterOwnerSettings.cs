using Application.Owners.Abstractions;
using Application.Owners.Enums;
using Infrastructure.Owners.Configuration;
using Microsoft.Extensions.Options;

namespace Infrastructure.Owners;

// Decisión de negocio: el chatbot nunca envía ni valida códigos de
// verificación, tampoco para registrar al dueño (Bot). Telegram ya no exige
// proof por la misma razón. Staff solo si RegisterOwner:RequireContactProofs=true.
public sealed class ConfiguredRegisterOwnerSettings(
    IOptions<RegisterOwnerOptions> options) : IRegisterOwnerSettings
{
    private readonly RegisterOwnerOptions _options = options.Value;

    public bool RequireContactProofs => _options.RequireContactProofs;

    public bool RequiresContactProof(RegisterOwnerChannel channel) =>
        channel switch
        {
            RegisterOwnerChannel.Bot => false,
            RegisterOwnerChannel.Telegram => false,
            _ => _options.RequireContactProofs
        };
}
