using Application.Common.Abstractions;
using Application.Notifications.UseCases;
using Application.ProcedureOrders.Abstraction;
using Application.ProcedureOrders.UseCases;
using Application.Veterinarians.Abstraction;
using Domain.ProcedureOrders.Entities;
using Domain.Veterinarians.Entities;
using MediatR;
using NSubstitute;
using Xunit;

namespace Application.Tests.MedicalOrders;

public class ProcedureOrderCommandHandlerTests
{
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IProcedureOrderRepository _orderRepo = Substitute.For<IProcedureOrderRepository>();
    private readonly IVeterinarianRepository _vetRepo = Substitute.For<IVeterinarianRepository>();
    private readonly ISender _sender = Substitute.For<ISender>();
    private readonly CreateProcedureOrderCommandHandler _createHandler;
    private readonly CompleteProcedureOrderCommandHandler _completeHandler;

    public ProcedureOrderCommandHandlerTests()
    {
        _unitOfWork.ProcedureOrdersRepository.Returns(_orderRepo);
        _unitOfWork.VeterinariansRepository.Returns(_vetRepo);
        _createHandler = new CreateProcedureOrderCommandHandler(_unitOfWork);
        _completeHandler = new CompleteProcedureOrderCommandHandler(_unitOfWork, _sender);
    }

    [Fact]
    public async Task CreateInHouseProcedureOrder_Succeeds()
    {
        var command = new CreateProcedureOrderCommand(
            ClientPetId: Guid.NewGuid(),
            VeterinarianId: Guid.NewGuid(),
            AppointmentId: Guid.NewGuid(),
            IsInHouse: true,
            ReferredTo: null,
            ReferralReason: null,
            Items: new List<ProcedureOrderItemInput>
            {
                new(Guid.NewGuid(), "Ecografía abdominal completa")
            });

        var result = await _createHandler.Handle(command, CancellationToken.None);

        Assert.NotNull(result);
        Assert.True(result.IsInHouse);
        Assert.Equal("Pendiente", result.Status);
        Assert.Single(result.Items);

        await _orderRepo.Received(1).AddAsync(Arg.Any<ProcedureOrder>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CompleteProcedureOrder_SetsStatusAndDispatchesNotification()
    {
        var vetId = Guid.NewGuid();
        var vetUserId = Guid.NewGuid();
        var appointmentId = Guid.NewGuid();

        var veterinarian = new Veterinarian(vetUserId, Guid.NewGuid(), "LIC-12345");
        // Forzar Id coincidente
        typeof(Veterinarian).GetProperty("Id")?.SetValue(veterinarian, vetId);

        var order = new ProcedureOrder(
            Guid.NewGuid(),
            vetId,
            appointmentId,
            isInHouse: true,
            referredTo: null,
            referralReason: null,
            items: new[] { (Guid.NewGuid(), (string?)"Prueba X") });

        _orderRepo.GetByIdAsync(order.Id, Arg.Any<CancellationToken>())
            .Returns(order);

        _vetRepo.GetByIdAsync(vetId, Arg.Any<CancellationToken>())
            .Returns(veterinarian);

        var resultFileUrl = "https://clinica.com/resultados/ecografia_123.pdf";

        await _completeHandler.Handle(
            new CompleteProcedureOrderCommand(order.Id, resultFileUrl),
            CancellationToken.None);

        Assert.Equal("Completada", order.Status);
        Assert.Equal(resultFileUrl, order.ResultFileUrl);

        await _orderRepo.Received(1).UpdateAsync(order, Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());

        // Verificar que se dispare la notificación al UserId del veterinario original
        await _sender.Received(1).Send(
            Arg.Is<CreateNotificationCommand>(cmd =>
                cmd.UserId == vetUserId &&
                cmd.AppointmentId == appointmentId &&
                cmd.Type == "ProcedimientoComp"),
            Arg.Any<CancellationToken>());
    }
}
