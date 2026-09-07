using Application.Owners.Abstractions;
using Application.Owners.Enums;
using Infrastructure.Owners.Configuration;
using Microsoft.Extensions.Options;

namespace Infrastructure.Owners;

public sealed class ConfiguredRegisterOwnerSettings(
    IOptions<RegisterOwnerOptions> options) : IRegisterOwnerSettings
{
    private readonly RegisterOwnerOptions _options = options.Value;

    public bool RequireContactProofs => _options.RequireContactProofs;

    public bool RequiresContactProof(RegisterOwnerChannel channel) =>
        channel is RegisterOwnerChannel.Bot or RegisterOwnerChannel.Telegram
        || _options.RequireContactProofs;
}
