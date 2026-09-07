using Application.Verification.Abstractions;
using Domain.Verification.Enums;

namespace Api.Tests.Verification.Support;

public sealed class FakeVerificationCodeSender : IVerificationCodeSender
{
    public VerificationDeliveryChannel Channel => VerificationDeliveryChannel.Email;

    public List<SentVerificationMessage> SentMessages { get; } = [];

    public Task SendAsync(
        string destination,
        string code,
        DateTimeOffset expiresAt,
        CancellationToken cancellationToken)
    {
        SentMessages.Add(new SentVerificationMessage(destination, code, expiresAt));
        return Task.CompletedTask;
    }
}

public sealed record SentVerificationMessage(
    string Destination,
    string Code,
    DateTimeOffset ExpiresAt);
