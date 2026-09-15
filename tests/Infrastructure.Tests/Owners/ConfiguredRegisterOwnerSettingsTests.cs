using Application.Owners.Enums;
using Infrastructure.Owners;
using Infrastructure.Owners.Configuration;
using Microsoft.Extensions.Options;
using Xunit;

namespace Infrastructure.Tests.Owners;

public sealed class ConfiguredRegisterOwnerSettingsTests
{
    [Fact]
    public void Bot_and_telegram_never_require_proof()
    {
        var settings = new ConfiguredRegisterOwnerSettings(
            Options.Create(new RegisterOwnerOptions { RequireContactProofs = false }));

        Assert.False(settings.RequireContactProofs);
        Assert.False(settings.RequiresContactProof(RegisterOwnerChannel.Staff));
        // Decisión de negocio: el canal Bot nunca envía ni valida códigos de verificación.
        Assert.False(settings.RequiresContactProof(RegisterOwnerChannel.Bot));
        Assert.False(settings.RequiresContactProof(RegisterOwnerChannel.Telegram));
    }

    [Fact]
    public void Staff_requires_proof_when_flag_is_true()
    {
        var settings = new ConfiguredRegisterOwnerSettings(
            Options.Create(new RegisterOwnerOptions { RequireContactProofs = true }));

        Assert.True(settings.RequiresContactProof(RegisterOwnerChannel.Staff));
    }
}
