using Application.Owners.Enums;
using Infrastructure.Owners;
using Infrastructure.Owners.Configuration;
using Microsoft.Extensions.Options;
using Xunit;

namespace Infrastructure.Tests.Owners;

public sealed class ConfiguredRegisterOwnerSettingsTests
{
    [Fact]
    public void Bot_and_telegram_always_require_proof()
    {
        var settings = new ConfiguredRegisterOwnerSettings(
            Options.Create(new RegisterOwnerOptions { RequireContactProofs = false }));

        Assert.False(settings.RequireContactProofs);
        Assert.False(settings.RequiresContactProof(RegisterOwnerChannel.Staff));
        Assert.True(settings.RequiresContactProof(RegisterOwnerChannel.Bot));
        Assert.True(settings.RequiresContactProof(RegisterOwnerChannel.Telegram));
    }

    [Fact]
    public void Staff_requires_proof_when_flag_is_true()
    {
        var settings = new ConfiguredRegisterOwnerSettings(
            Options.Create(new RegisterOwnerOptions { RequireContactProofs = true }));

        Assert.True(settings.RequiresContactProof(RegisterOwnerChannel.Staff));
    }
}
