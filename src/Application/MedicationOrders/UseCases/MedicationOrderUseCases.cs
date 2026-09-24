using Application.Common.Abstractions;
using Application.Common.Exceptions;
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
    Guid? HospitalizationStayId = null,
    Guid? RequestingUserId = null) : IRequest<MedicationOrder>;

public sealed record CompleteMedicationOrderCommand(Guid Id) : IRequest<Unit>;

public sealed record GetMedicationOrderByIdQuery(Guid Id) : IRequest<MedicationOrder>;

public sealed record GetMedicationOrdersByAppointmentIdQuery(Guid AppointmentId) : IRequest<IEnumerable<MedicationOrder>>;

public sealed record GetMedicationOrdersByHospitalizationStayIdQuery(Guid HospitalizationStayId) : IRequest<IEnumerable<MedicationOrder>>;

public sealed record GetPendingMedicationOrdersQuery() : IRequest<IEnumerable<MedicationOrder>>;

// Handlers
public sealed class CreateMedicationOrderCommandHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<CreateMedicationOrderCommand, MedicationOrder>
{
    public async Task<MedicationOrder> Handle(
        CreateMedicationOrderCommand request,
        CancellationToken cancellationToken)
    {
        if ((request.AppointmentId is null) == (request.HospitalizationStayId is null))
        {
            throw new BadRequestException("La orden debe tener exactamente una cita o una estancia de hospitalización.");
        }

        Guid veterinarianId;
        if (request.AppointmentId is Guid appointmentId)
        {
            var appointment = await unitOfWork.AppointmentsRepository.GetByIdAsync(appointmentId, cancellationToken)
                ?? throw new NotFoundException($"No se encontró la cita con ID '{appointmentId}'.");

            if (appointment.ClientPetId != request.ClientPetId)
            {
                throw new BadRequestException("La cita no pertenece a la mascota indicada.");
            }

            veterinarianId = appointment.VeterinarianId;
        }
        else
        {
            var stay = await unitOfWork.HospitalizationStaysRepository.GetByIdAsync(
                request.HospitalizationStayId!.Value,
                cancellationToken)
                ?? throw new NotFoundException("La estancia de hospitalización no existe.");

            if (stay.Estado != Domain.HospitalizationStays.Entities.HospitalizationStayStatus.Activa)
            {
                throw new BadRequestException("No se pueden crear órdenes para una estancia dada de alta.");
            }

            if (stay.ClientPetId != request.ClientPetId)
            {
                throw new BadRequestException("La estancia no pertenece a la mascota indicada.");
            }

            if (stay.AppointmentId is Guid stayAppointmentId)
            {
                var appointment = await unitOfWork.AppointmentsRepository.GetByIdAsync(stayAppointmentId, cancellationToken)
                    ?? throw new NotFoundException("La cita de origen de la estancia no existe.");
                veterinarianId = appointment.VeterinarianId;
            }
            else
            {
                if (request.RequestingUserId is not Guid requestingUserId)
                {
                    throw new BadRequestException("Se requiere un usuario autenticado para crear una orden directa.");
                }

                var veterinarian = await unitOfWork.VeterinariansRepository.GetByUserIdAsync(requestingUserId, cancellationToken)
                    ?? throw new BadRequestException("El usuario autenticado no tiene un perfil de veterinario.");
                veterinarianId = veterinarian.Id;
            }
        }

        var itemsTuple = request.Items is not null && !request.IsInHouse
            ? request.Items.Select(item => (item.MedicationId, item.Notes, UnitPrice: 0m)).ToList()
            : new List<(Guid MedicationId, string? Notes, decimal UnitPrice)>();
        if (request.IsInHouse && request.Items is not null)
        {
            foreach (var item in request.Items)
            {
                var medication = await unitOfWork.MedicationsRepository.GetByIdAsync(item.MedicationId, cancellationToken)
                    ?? throw new NotFoundException($"No se encontró el medicamento con ID '{item.MedicationId}'.");

                if (!medication.IsActive)
                {
                    throw new BadRequestException("No se puede ordenar un medicamento inactivo.");
                }

                itemsTuple.Add((medication.Id, item.Notes, medication.Price));
            }
        }

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

public sealed class GetMedicationOrdersByHospitalizationStayIdQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetMedicationOrdersByHospitalizationStayIdQuery, IEnumerable<MedicationOrder>>
{
    public async Task<IEnumerable<MedicationOrder>> Handle(
        GetMedicationOrdersByHospitalizationStayIdQuery request,
        CancellationToken cancellationToken)
        => await unitOfWork.MedicationOrdersRepository.GetByHospitalizationStayIdAsync(
            request.HospitalizationStayId,
            cancellationToken);
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
            .Must(x => (x.AppointmentId is null) != (x.HospitalizationStayId is null))
            .WithMessage("Debe indicar exactamente una cita o una estancia de hospitalización.");

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
