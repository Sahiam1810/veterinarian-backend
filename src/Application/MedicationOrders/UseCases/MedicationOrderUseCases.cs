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

        if (request.AppointmentId is Guid appointmentId)
        {
            var appointment = await unitOfWork.AppointmentsRepository.GetByIdAsync(appointmentId, cancellationToken);
            if (appointment is null)
            {
                throw new NotFoundException($"No se encontró la cita con ID '{appointmentId}'.");
            }

            veterinarianId = appointment.VeterinarianId;
        }
        else if (request.HospitalizationStayId is Guid stayId)
        {
            var stay = await unitOfWork.HospitalizationStaysRepository.GetByIdAsync(stayId, cancellationToken)
                ?? throw new NotFoundException($"No se encontró la estancia con ID '{stayId}'.");

            if (stay.Estado != HospitalizationStayStatus.Activa)
            {
                throw new InvalidOperationException("No se pueden crear órdenes en una estancia dada de alta.");
            }

            if (stay.ClientPetId != request.ClientPetId)
            {
                throw new BadRequestException("La estancia y la mascota de la orden deben coincidir.");
            }

            veterinarianId = await ResolveVeterinarianIdForStayAsync(stay, cancellationToken)
                ?? stay.AdmittedByUserId;
        }
        else
        {
            throw new BadRequestException("La orden debe tener una cita o una estancia como origen.");
        }

        var itemsTuple = request.Items?.Select(i => (i.MedicationId, i.Notes)).ToList() ?? new List<(Guid MedicationId, string? Notes)>();
        var medicationOrder = new MedicationOrder(
            request.ClientPetId,
            veterinarianId,
            request.AppointmentId,
            request.IsInHouse,
            request.ReferredTo,
            request.ReferralReason,
            itemsTuple,
            request.HospitalizationStayId);

        foreach (var item in medicationOrder.Items)
        {
            var medication = await unitOfWork.MedicationsRepository.GetByIdAsync(item.MedicationId, cancellationToken);
            if (medication is not null)
            {
                item.SetUnitPrice(medication.Price);
            }
        }

        await unitOfWork.MedicationOrdersRepository.AddAsync(medicationOrder, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return medicationOrder;
    }

    private async Task<Guid?> ResolveVeterinarianIdForStayAsync(HospitalizationStay stay, CancellationToken cancellationToken)
    {
        if (stay.AppointmentId is Guid appointmentId)
        {
            var appointment = await unitOfWork.AppointmentsRepository.GetByIdAsync(appointmentId, cancellationToken);
            return appointment?.VeterinarianId;
        }

        return null;
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
            .Must(x => x.AppointmentId.HasValue || x.HospitalizationStayId.HasValue)
            .WithMessage("La orden debe tener una cita o una estancia como origen.");

        RuleFor(x => x)
            .Must(x => !(x.AppointmentId.HasValue && x.HospitalizationStayId.HasValue))
            .WithMessage("La orden no puede tener cita y estancia al mismo tiempo.");

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
