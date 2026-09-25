using Application.Common.Abstractions;
using Application.Common.Exceptions;
using Application.MedicalOrders;
using Domain.MedicalOrders;
using Domain.MedicationOrders.Entities;
using FluentValidation;
using MediatR;

namespace Application.MedicationOrders.UseCases;

public sealed record MedicationOrderItemInput(Guid MedicationId, string? Notes);

// Origen: exactamente uno de AppointmentId o HospitalizationStayId.
// ActorUserId: usuario autenticado; firma como veterinario las órdenes de hospitalización.
public sealed record CreateMedicationOrderCommand(
    Guid ClientPetId,
    Guid? AppointmentId,
    bool IsInHouse,
    string? ReferredTo,
    string? ReferralReason,
    List<MedicationOrderItemInput>? Items,
    Guid? HospitalizationStayId = null,
    Guid ActorUserId = default) : IRequest<MedicationOrder>;

public sealed record CompleteMedicationOrderCommand(Guid Id) : IRequest<Unit>;

public sealed record GetMedicationOrderByIdQuery(Guid Id) : IRequest<MedicationOrder>;

public sealed record GetMedicationOrdersByAppointmentIdQuery(Guid AppointmentId) : IRequest<IEnumerable<MedicationOrder>>;

public sealed record GetPendingMedicationOrdersQuery() : IRequest<IEnumerable<MedicationOrder>>;

public sealed record GetMedicationOrdersByHospitalizationStayIdQuery(Guid HospitalizationStayId) : IRequest<IEnumerable<MedicationOrder>>;

// Handlers
public sealed class CreateMedicationOrderCommandHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<CreateMedicationOrderCommand, MedicationOrder>
{
    public async Task<MedicationOrder> Handle(
        CreateMedicationOrderCommand request,
        CancellationToken cancellationToken)
    {
        var veterinarianId = await MedicalOrderOriginResolver.ResolveVeterinarianIdAsync(
            unitOfWork,
            request.ClientPetId,
            request.AppointmentId,
            request.HospitalizationStayId,
            request.ActorUserId,
            cancellationToken);

        var itemsTuple = request.Items?.Select(i => (i.MedicationId, i.Notes));

        var medicationOrder = new MedicationOrder(
            request.ClientPetId,
            veterinarianId,
            request.AppointmentId,
            request.IsInHouse,
            request.ReferredTo,
            request.ReferralReason,
            itemsTuple,
            request.HospitalizationStayId);

        await unitOfWork.MedicationOrdersRepository.AddAsync(medicationOrder, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return medicationOrder;
    }
}

public sealed class CompleteMedicationOrderCommandHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<CompleteMedicationOrderCommand, Unit>
{
    public async Task<Unit> Handle(
        CompleteMedicationOrderCommand request,
        CancellationToken cancellationToken)
    {
        var order = await unitOfWork.MedicationOrdersRepository.GetByIdAsync(request.Id, cancellationToken);
        if (order is null)
        {
            throw new NotFoundException($"No se encontró la orden de medicamento con ID '{request.Id}'.");
        }

        // Solo Pendiente → Entregada; si ya cambió, 409 sin tocar la orden.
        if (!order.IsPending)
        {
            throw new ConflictException(order.DescribeInvalidTransition());
        }

        foreach (var item in order.Items)
        {
            var medication = await unitOfWork.MedicationsRepository.GetByIdAsync(item.MedicationId, cancellationToken);
            item.SetUnitPrice(medication?.Price);
        }

        order.Complete();
        await unitOfWork.MedicationOrdersRepository.UpdateAsync(order, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}

public sealed class GetMedicationOrderByIdQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetMedicationOrderByIdQuery, MedicationOrder>
{
    public async Task<MedicationOrder> Handle(
        GetMedicationOrderByIdQuery request,
        CancellationToken cancellationToken)
    {
        var order = await unitOfWork.MedicationOrdersRepository.GetByIdAsync(request.Id, cancellationToken);
        if (order is null)
        {
            throw new NotFoundException($"No se encontró la orden de medicamento con ID '{request.Id}'.");
        }

        return order;
    }
}

public sealed class GetMedicationOrdersByAppointmentIdQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetMedicationOrdersByAppointmentIdQuery, IEnumerable<MedicationOrder>>
{
    public async Task<IEnumerable<MedicationOrder>> Handle(
        GetMedicationOrdersByAppointmentIdQuery request,
        CancellationToken cancellationToken)
    {
        return await unitOfWork.MedicationOrdersRepository.GetByAppointmentIdAsync(request.AppointmentId, cancellationToken);
    }
}

public sealed class GetPendingMedicationOrdersQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetPendingMedicationOrdersQuery, IEnumerable<MedicationOrder>>
{
    public async Task<IEnumerable<MedicationOrder>> Handle(
        GetPendingMedicationOrdersQuery request,
        CancellationToken cancellationToken)
    {
        return await unitOfWork.MedicationOrdersRepository.GetPendingAsync(cancellationToken);
    }
}

public sealed class GetMedicationOrdersByHospitalizationStayIdQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetMedicationOrdersByHospitalizationStayIdQuery, IEnumerable<MedicationOrder>>
{
    // Mismo criterio que la factura: órdenes de la estancia + las de su cita de origen.
    public async Task<IEnumerable<MedicationOrder>> Handle(
        GetMedicationOrdersByHospitalizationStayIdQuery request,
        CancellationToken cancellationToken)
    {
        var stay = await unitOfWork.HospitalizationStaysRepository.GetByIdAsync(
            request.HospitalizationStayId,
            cancellationToken)
            ?? throw new NotFoundException(
                $"No se encontró la estancia de hospitalización con ID '{request.HospitalizationStayId}'.");

        return await unitOfWork.MedicationOrdersRepository.GetByHospitalizationStayAsync(
            stay.Id,
            stay.AppointmentId,
            cancellationToken);
    }
}

// Validator
public sealed class CreateMedicationOrderCommandValidator : AbstractValidator<CreateMedicationOrderCommand>
{
    public CreateMedicationOrderCommandValidator()
    {
        RuleFor(x => x.ClientPetId)
            .NotEmpty().WithMessage("El paciente es obligatorio.");

        RuleFor(x => x)
            .Must(x => MedicalOrderOriginRules.HasExactlyOneOrigin(x.AppointmentId, x.HospitalizationStayId))
            .WithName("AppointmentId")
            .WithMessage(MedicalOrderOriginRules.ExactlyOneOriginMessage);

        When(x => x.IsInHouse, () =>
        {
            RuleFor(x => x.Items)
                .NotNull().WithMessage("Debe incluir al menos un ítem de medicamento en una orden interna.")
                .Must(items => items != null && items.Count > 0)
                .WithMessage("Debe incluir al menos un ítem de medicamento en una orden interna.");

            RuleFor(x => x.ReferredTo)
                .Must(string.IsNullOrWhiteSpace)
                .WithMessage("Una orden interna no debe especificar destino de remisión.");

            RuleFor(x => x.ReferralReason)
                .Must(string.IsNullOrWhiteSpace)
                .WithMessage("Una orden interna no debe especificar motivo de remisión.");
        });

        When(x => !x.IsInHouse, () =>
        {
            RuleFor(x => x.ReferredTo)
                .NotEmpty().WithMessage("El lugar de remisión es obligatorio en órdenes externas.");

            RuleFor(x => x.ReferralReason)
                .NotEmpty().WithMessage("El motivo de remisión es obligatorio en órdenes externas.");

            RuleFor(x => x.Items)
                .Must(items => items == null || items.Count == 0)
                .WithMessage("Una orden externa remitida no debe contener ítems de catálogo interno.");
        });
    }
}
