using Application.Clients.Abstraction;
using Application.ContactVerification.Abstractions;
using Application.ContactVerification.Errors;
using Application.Telegram.Abstractions;
using Application.Telegram.Errors;
using Application.Telegram.Linking;
using Domain.ContactVerification.Enums;
using Domain.Clients.Entities;
using Domain.Telegram.Entities;
using NSubstitute;
using Xunit;

namespace Application.Tests.Telegram;

public sealed class LinkTelegramBotAccountWithProofHandlerTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 9, 14, 12, 0, 0, TimeSpan.Zero);
    private const long TelegramUserId = 555;

    [Fact]
    public async Task Valid_claim_proof_creates_the_link_and_returns_client_name()
    {
        var fixture = CreateFixture();
        var client = new ClientEntity("Ana Dueña", "ana@huellitas.test", "1234567890", "3001234567", "Calle 1");
        fixture.Clients.GetByIdAsync(client.Id, fixture.Token).Returns(client);
        fixture.UserLinks.GetByTelegramUserIdAsync(TelegramUserId, fixture.Token)
            .Returns((TelegramUserLink?)null);
        fixture.UserLinks.GetByClientIdAsync(client.Id, fixture.Token)
            .Returns((TelegramUserLink?)null);
        fixture.ProofConsumer.ConsumeAsync(Arg.Any<ConsumeContactVerificationProof>(), fixture.Token)
            .Returns(new ConsumedContactVerificationProof(
                Guid.NewGuid(),
                ContactVerificationPurpose.Claim,
                client.Id,
                "destination-hash"));

        var result = await CreateHandler(fixture).Handle(
            new LinkTelegramBotAccountWithProofCommand(Guid.NewGuid(), "proof", TelegramUserId),
            fixture.Token);

        Assert.Equal(client.FullName.Value, result.FullName);
        Assert.NotEqual(Guid.Empty, result.LinkId);
        await fixture.UnitOfWork.Received(1).SaveChangesAsync(fixture.Token);
    }

    [Theory]
    [InlineData(ContactVerificationPurpose.Register, true)]
    [InlineData(ContactVerificationPurpose.Claim, false)]
    public async Task Claim_rejects_wrong_purpose_or_missing_subject(
        ContactVerificationPurpose purpose,
        bool hasSubject)
    {
        var fixture = CreateFixture();
        fixture.ProofConsumer.ConsumeAsync(Arg.Any<ConsumeContactVerificationProof>(), fixture.Token)
            .Returns(new ConsumedContactVerificationProof(
                Guid.NewGuid(), purpose, hasSubject ? Guid.NewGuid() : null, "destination-hash"));

        await Assert.ThrowsAsync<ContactVerificationException>(() => CreateHandler(fixture).Handle(
            new LinkTelegramBotAccountWithProofCommand(Guid.NewGuid(), "proof", TelegramUserId),
            fixture.Token));

        await fixture.Clients.DidNotReceive().GetByIdAsync(Arg.Any<Guid>(), fixture.Token);
    }

    [Fact]
    public async Task Claim_rejects_inactive_client()
    {
        var fixture = CreateFixture();
        var client = new ClientEntity("Ana Dueña", "ana@huellitas.test", "1234567890", "3001234567", "Calle 1");
        client.Deactivate();
        fixture.Clients.GetByIdAsync(client.Id, fixture.Token).Returns(client);
        fixture.ProofConsumer.ConsumeAsync(Arg.Any<ConsumeContactVerificationProof>(), fixture.Token)
            .Returns(Consumed(client.Id));

        await Assert.ThrowsAsync<TelegramAccountUnavailableException>(() => CreateHandler(fixture).Handle(
            new LinkTelegramBotAccountWithProofCommand(Guid.NewGuid(), "proof", TelegramUserId),
            fixture.Token));
    }

    [Fact]
    public async Task Claim_rejects_telegram_linked_to_another_client()
    {
        var fixture = CreateFixture();
        var client = new ClientEntity("Ana Dueña", "ana@huellitas.test", "1234567890", "3001234567", "Calle 1");
        fixture.Clients.GetByIdAsync(client.Id, fixture.Token).Returns(client);
        fixture.UserLinks.GetByTelegramUserIdAsync(TelegramUserId, fixture.Token)
            .Returns(TelegramUserLink.Create(Guid.NewGuid(), TelegramUserId, TelegramUserId, Now.UtcDateTime));
        fixture.ProofConsumer.ConsumeAsync(Arg.Any<ConsumeContactVerificationProof>(), fixture.Token)
            .Returns(Consumed(client.Id));

        await Assert.ThrowsAsync<TelegramIdentityConflictException>(() => CreateHandler(fixture).Handle(
            new LinkTelegramBotAccountWithProofCommand(Guid.NewGuid(), "proof", TelegramUserId),
            fixture.Token));
    }

    [Fact]
    public async Task Retry_with_same_telegram_user_is_idempotent()
    {
        var fixture = CreateFixture();
        var client = new ClientEntity("Ana Dueña", "ana@huellitas.test", "1234567890", "3001234567", "Calle 1");
        var existingLink = TelegramUserLink.Create(client.Id, TelegramUserId, TelegramUserId, Now.UtcDateTime);
        fixture.Clients.GetByIdAsync(client.Id, fixture.Token).Returns(client);
        fixture.UserLinks.GetByTelegramUserIdAsync(TelegramUserId, fixture.Token)
            .Returns(existingLink);
        fixture.ProofConsumer.ConsumeAsync(Arg.Any<ConsumeContactVerificationProof>(), fixture.Token)
            .Returns(Consumed(client.Id));

        var result = await CreateHandler(fixture).Handle(
            new LinkTelegramBotAccountWithProofCommand(Guid.NewGuid(), "proof", TelegramUserId),
            fixture.Token);

        Assert.Equal(existingLink.Id, result.LinkId);
        await fixture.UserLinks.DidNotReceive().AddAsync(
            Arg.Any<TelegramUserLink>(), Arg.Any<CancellationToken>());
        await fixture.UserLinks.DidNotReceive().UpdateAsync(
            Arg.Any<TelegramUserLink>(), Arg.Any<CancellationToken>());
    }

    private static LinkTelegramBotAccountWithProofHandler CreateHandler(Fixture fixture) =>
        new(
            fixture.ProofConsumer,
            new TelegramBotAccountLinker(fixture.UnitOfWork, fixture.TimeProvider));

    private static ConsumedContactVerificationProof Consumed(Guid clientId) =>
        new(Guid.NewGuid(), ContactVerificationPurpose.Claim, clientId, "destination-hash");

    private static Fixture CreateFixture()
    {
        var unitOfWork = Substitute.For<ITelegramUnitOfWork>();
        var clients = Substitute.For<IClientRepository>();
        var userLinks = Substitute.For<ITelegramUserLinkRepository>();
        unitOfWork.ClientsRepository.Returns(clients);
        unitOfWork.UserLinksRepository.Returns(userLinks);

        return new Fixture(
            unitOfWork,
            clients,
            userLinks,
            Substitute.For<IConsumeContactVerificationProof>(),
            new FixedTimeProvider(Now),
            CancellationToken.None);
    }

    private sealed record Fixture(
        ITelegramUnitOfWork UnitOfWork,
        IClientRepository Clients,
        ITelegramUserLinkRepository UserLinks,
        IConsumeContactVerificationProof ProofConsumer,
        TimeProvider TimeProvider,
        CancellationToken Token);

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}