using Application.Common.Abstractions;
using Application.Common.Exceptions;
using Domain.HospitalizationStays.Entities;
using Domain.MedicationOrders.Entities;
using FluentValidation;
using MediatR;

namespace Application.MedicationOrders.UseCases;

public sealed record MedicationOrderItemInput(Guid MedicationId, string? Notes);

public sealed record CreateMedicationOrderCommand(
    Guid ClientPetId,
    Guid? AppointmentId,
    bool IsInHouse,
    string? ReferredTo,
    string? ReferralReason,
    List<MedicationOrderItemInput>? Items,
    Guid? HospitalizationStayId = null) : IRequest<MedicationOrder>;

public sealed record CompleteMedicationOrderCommand(Guid Id) : IRequest<Unit>;

public sealed record GetMedicationOrderByIdQuery(Guid Id) : IRequest<MedicationOrder>;

public sealed record GetMedicationOrdersByAppointmentIdQuery(Guid AppointmentId) : IRequest<IEnumerable<MedicationOrder>>;

public sealed record GetPendingMedicationOrdersQuery() : IRequest<IEnumerable<MedicationOrder>>;

// Handlers
public sealed class CreateMedicationOrderCommandHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<CreateMedicationOrderCommand, MedicationOrder>
{
    public async Task<MedicationOrder> Handle(
        CreateMedicationOrderCommand request,
        CancellationToken cancellationToken)
    {
        Guid veterinarianId;

        if (request.AppointmentId is Guid appointmentId && appointmentId != Guid.Empty)
        {
            var appointment = await unitOfWork.AppointmentsRepository.GetByIdAsync(appointmentId, cancellationToken);
            if (appointment is null)
            {
                throw new NotFoundException($"No se encontró la cita con ID '{appointmentId}'.");
            }

            if (appointment.ClientPetId != request.ClientPetId)
            {
                throw new BadRequestException("La mascota de la orden no coincide con la cita.");
            }

            veterinarianId = appointment.VeterinarianId;
        }
        else if (request.HospitalizationStayId is Guid stayId && stayId != Guid.Empty)
        {
            var stay = await unitOfWork.HospitalizationStaysRepository.GetByIdAsync(stayId, cancellationToken);
            if (stay is null)
            {
                throw new NotFoundException($"No se encontró la estancia de hospitalización con ID '{stayId}'.");
            }

            if (stay.Estado != HospitalizationStayStatus.Activa)
            {
                throw new ConflictException("No se puede crear una orden en una estancia dada de alta.");
            }

            if (stay.ClientPetId != request.ClientPetId)
            {
                throw new BadRequestException("La mascota de la orden no coincide con la estancia.");
            }

            veterinarianId = stay.AdmittedByUserId;
        }
        else
        {
            throw new BadRequestException("Debe especificar exactamente uno de los orígenes: AppointmentId o HospitalizationStayId.");
        }

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

        if (string.Equals(order.Status, "Entregada", StringComparison.OrdinalIgnoreCase))
        {
            throw new ConflictException("La orden de medicamento ya se encuentra entregada.");
        }

        if (string.Equals(order.Status, "Cancelada", StringComparison.OrdinalIgnoreCase))
        {
            throw new ConflictException("No se puede entregar una orden de medicamento cancelada.");
        }

        if (!string.Equals(order.Status, "Pendiente", StringComparison.OrdinalIgnoreCase))
        {
            throw new ConflictException("Solo se puede entregar una orden de medicamento pendiente.");
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

// Validator
public sealed class CreateMedicationOrderCommandValidator : AbstractValidator<CreateMedicationOrderCommand>
{
    public CreateMedicationOrderCommandValidator()
    {
        RuleFor(x => x.ClientPetId)
            .NotEmpty().WithMessage("El paciente es obligatorio.");

        RuleFor(x => x)
            .Must(x => (x.AppointmentId.HasValue && x.AppointmentId.Value != Guid.Empty) ^ (x.HospitalizationStayId.HasValue && x.HospitalizationStayId.Value != Guid.Empty))
            .WithMessage("Debe especificar exactamente uno de los orígenes: AppointmentId o HospitalizationStayId.");

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

