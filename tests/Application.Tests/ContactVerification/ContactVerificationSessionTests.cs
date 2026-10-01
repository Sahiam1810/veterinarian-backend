using Domain.ContactVerification.Entities;
using Domain.ContactVerification.Enums;
using Xunit;

namespace Application.Tests.ContactVerification;

public sealed class ContactVerificationSessionTests
{
    private const string ValidHash = "ABABABABABABABABABABABABABABABABABABABABABABABABABABABABABABABAB";
    private const string ValidOtpHash = "CDCDCDCDCDCDCDCDCDCDCDCDCDCDCDCDCDCDCDCDCDCDCDCDCDCDCDCDCDCDCDCD";
    private const string ValidProofHash = "EFEFEFEFEFEFEFEFEFEFEFEFEFEFEFEFEFEFEFEFEFEFEFEFEFEFEFEFEFEFEFEF";

    private static readonly DateTime CreatedAt = new(2026, 9, 7, 12, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime ExpiresAt = CreatedAt.AddMinutes(10);

    [Fact]
    public void Start_creates_register_session_awaiting_otp_without_plaintext()
    {
        var session = ContactVerificationSession.Start(
            ContactVerificationPurpose.Register,
            ContactVerificationChannel.Email,
            ValidHash,
            ValidOtpHash,
            ExpiresAt,
            CreatedAt);

        Assert.NotEqual(Guid.Empty, session.Id);
        Assert.Equal(ContactVerificationPurpose.Register, session.Purpose);
        Assert.Equal(ContactVerificationChannel.Email, session.Channel);
        Assert.Equal(ValidHash, session.DestinationHash);
        Assert.Null(session.SubjectUserId);
        Assert.Equal(ValidOtpHash, session.OtpHash);
        Assert.Null(session.ProofHash);
        Assert.Equal(ContactVerificationSessionStatus.AwaitingOtp, session.Status);
        Assert.True(session.IsAlive);
    }

    [Fact]
    public void Start_claim_requires_subject_user()
    {
        Assert.Throws<ArgumentException>(() => ContactVerificationSession.Start(
            ContactVerificationPurpose.Claim,
            ContactVerificationChannel.Email,
            ValidHash,
            ValidOtpHash,
            ExpiresAt,
            CreatedAt));
    }

    [Fact]
    public void Start_register_rejects_subject_user()
    {
        Assert.Throws<ArgumentException>(() => ContactVerificationSession.Start(
            ContactVerificationPurpose.Register,
            ContactVerificationChannel.Email,
            ValidHash,
            ValidOtpHash,
            ExpiresAt,
            CreatedAt,
            Guid.NewGuid()));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("tooshort")]
    public void Start_rejects_non_sha256_hashes(string? invalid)
    {
        Assert.Throws<ArgumentException>(() => ContactVerificationSession.Start(
            ContactVerificationPurpose.Register,
            ContactVerificationChannel.Email,
            invalid!,
            ValidOtpHash,
            ExpiresAt,
            CreatedAt));
    }

    [Fact]
    public void IssueProof_clears_otp_hash_and_stores_only_proof_hash()
    {
        var session = StartRegister();
        var confirmedAt = CreatedAt.AddMinutes(1);

        session.IssueProof(ValidProofHash, confirmedAt.AddMinutes(15), confirmedAt);

        Assert.Equal(ContactVerificationSessionStatus.ProofIssued, session.Status);
        Assert.Null(session.OtpHash);
        Assert.Equal(ValidProofHash, session.ProofHash);
        Assert.True(session.IsAlive);
    }

    [Fact]
    public void ConsumeProof_is_single_use()
    {
        var session = StartRegister();
        var confirmedAt = CreatedAt.AddMinutes(1);
        session.IssueProof(ValidProofHash, confirmedAt.AddMinutes(15), confirmedAt);

        session.ConsumeProof(confirmedAt.AddMinutes(2));

        Assert.Equal(ContactVerificationSessionStatus.Consumed, session.Status);
        Assert.Null(session.ProofHash);
        Assert.False(session.IsAlive);
        Assert.Throws<InvalidOperationException>(() => session.ConsumeProof(confirmedAt.AddMinutes(3)));
    }

    [Fact]
    public void RegisterFailedAttempt_blocks_and_clears_otp_hash()
    {
        var session = StartRegister();

        session.RegisterFailedAttempt(1, CreatedAt.AddSeconds(1));

        Assert.Equal(ContactVerificationSessionStatus.Blocked, session.Status);
        Assert.Null(session.OtpHash);
        Assert.False(session.IsAlive);
    }

    [Fact]
    public void Cancel_releases_active_session_without_keeping_secrets()
    {
        var session = StartRegister();

        session.Cancel(CreatedAt.AddSeconds(5));

        Assert.Equal(ContactVerificationSessionStatus.Cancelled, session.Status);
        Assert.Null(session.OtpHash);
        Assert.Null(session.ProofHash);
    }

    private static ContactVerificationSession StartRegister() =>
        ContactVerificationSession.Start(
            ContactVerificationPurpose.Register,
            ContactVerificationChannel.Email,
            ValidHash,
            ValidOtpHash,
            ExpiresAt,
            CreatedAt);
}
