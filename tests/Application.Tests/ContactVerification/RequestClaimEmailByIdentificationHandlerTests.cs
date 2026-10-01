using Application.Clients.Abstraction;
using Application.Common.Abstractions;
using Application.Common.Exceptions;
using Application.ContactVerification.Abstractions;
using Application.ContactVerification.UseCases;
using Domain.Clients.Entities;
using Domain.ContactVerification.Enums;
using NSubstitute;
using Xunit;

namespace Application.Tests.ContactVerification;

public sealed class RequestClaimEmailByIdentificationHandlerTests
{
    private readonly IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IClientRepository clients = Substitute.For<IClientRepository>();
    private readonly IRequestContactEmailVerification requestEmail =
        Substitute.For<IRequestContactEmailVerification>();

    private readonly RequestClaimEmailByIdentificationHandler sut;

    public RequestClaimEmailByIdentificationHandlerTests()
    {
        unitOfWork.ClientsRepository.Returns(clients);
        sut = new RequestClaimEmailByIdentificationHandler(unitOfWork, requestEmail);
    }

    [Fact]
    public async Task RequestAsync_when_client_exists_requests_claim_otp_with_server_email()
    {
        var client = new ClientEntity(
            "Ana Pérez",
            "ana@huellitas.test",
            "1234567890",
            "3001234567",
            null);
        clients.GetByIdentificationNumberAsync("1234567890", Arg.Any<CancellationToken>())
            .Returns(client);
        var sessionId = Guid.NewGuid();
        requestEmail.RequestClaimForClientAsync(
            Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new RequestContactEmailVerificationResult(
                sessionId,
                DateTime.UtcNow.AddMinutes(10),
                ContactVerificationChannel.Email));

        var result = await sut.RequestAsync(
            new RequestClaimEmailByIdentification("1234567890"),
            CancellationToken.None);

        Assert.Equal(sessionId, result.SessionId);
        Assert.Equal("a***@huellitas.test", result.MaskedEmail);
        await requestEmail.Received(1).RequestClaimForClientAsync(
            client.Id,
            "ana@huellitas.test",
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RequestAsync_when_client_missing_throws_not_found()
    {
        clients.GetByIdentificationNumberAsync("9999999999", Arg.Any<CancellationToken>())
            .Returns((ClientEntity?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            sut.RequestAsync(
                new RequestClaimEmailByIdentification("9999999999"),
                CancellationToken.None));
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("12")]
    [InlineData("")]
    public async Task RequestAsync_when_identification_invalid_throws_not_found(string identification)
    {
        await Assert.ThrowsAsync<NotFoundException>(() =>
            sut.RequestAsync(
                new RequestClaimEmailByIdentification(identification),
                CancellationToken.None));
    }

    [Fact]
    public void MaskEmail_keeps_first_local_char()
    {
        Assert.Equal("s***@correo.com", RequestClaimEmailByIdentificationHandler.MaskEmail("sam@correo.com"));
    }
}
