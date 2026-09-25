using Application.Common.Abstractions;
using Application.Common.Exceptions;
using Application.Notifications.UseCases;
using Domain.HospitalizationStays.Entities;
using Domain.ProcedureOrders.Entities;
using FluentValidation;
using MediatR;

namespace Application.ProcedureOrders.UseCases;

public sealed record ProcedureOrderItemInput(Guid ProcedureId, string? Notes);

public sealed record CreateProcedureOrderCommand(
    Guid ClientPetId,
    Guid? AppointmentId,
    bool IsInHouse,
    string? ReferredTo,
    string? ReferralReason,
    List<ProcedureOrderItemInput>? Items,
    Guid? HospitalizationStayId = null) : IRequest<ProcedureOrder>;

public sealed record CompleteProcedureOrderCommand(Guid Id, string? ResultFileUrl = null) : IRequest<Unit>;

public sealed record GetProcedureOrderByIdQuery(Guid Id) : IRequest<ProcedureOrder>;

public sealed record GetProcedureOrdersByAppointmentIdQuery(Guid AppointmentId) : IRequest<IEnumerable<ProcedureOrder>>;

public sealed record GetPendingProcedureOrdersQuery() : IRequest<IEnumerable<ProcedureOrder>>;

// Handlers
public sealed class CreateProcedureOrderCommandHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<CreateProcedureOrderCommand, ProcedureOrder>
{
    public async Task<ProcedureOrder> Handle(
        CreateProcedureOrderCommand request,
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

        var itemsTuple = request.Items?.Select(i => (i.ProcedureId, i.Notes));

        var procedureOrder = new ProcedureOrder(
            request.ClientPetId,
            veterinarianId,
            request.AppointmentId,
            request.IsInHouse,
            request.ReferredTo,
            request.ReferralReason,
            itemsTuple,
            request.HospitalizationStayId);

        await unitOfWork.ProcedureOrdersRepository.AddAsync(procedureOrder, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return procedureOrder;
    }
}

public sealed class CompleteProcedureOrderCommandHandler(
    IUnitOfWork unitOfWork,
    ISender sender)
    : IRequestHandler<CompleteProcedureOrderCommand, Unit>
{
    public async Task<Unit> Handle(
        CompleteProcedureOrderCommand request,
        CancellationToken cancellationToken)
    {
        var order = await unitOfWork.ProcedureOrdersRepository.GetByIdAsync(request.Id, cancellationToken);
        if (order is null)
        {
            throw new NotFoundException($"No se encontró la orden de procedimiento con ID '{request.Id}'.");
        }

        if (string.Equals(order.Status, "Completada", StringComparison.OrdinalIgnoreCase))
        {
            throw new ConflictException("La orden de procedimiento ya se encuentra completada.");
        }

        if (string.Equals(order.Status, "Cancelada", StringComparison.OrdinalIgnoreCase))
        {
            throw new ConflictException("No se puede completar una orden de procedimiento cancelada.");
        }

        if (!string.Equals(order.Status, "Pendiente", StringComparison.OrdinalIgnoreCase))
        {
            throw new ConflictException("Solo se puede completar una orden de procedimiento pendiente.");
        }

        order.Complete(request.ResultFileUrl);
        await unitOfWork.ProcedureOrdersRepository.UpdateAsync(order, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        // Notificación al veterinario que realizó la orden
        if (order.AppointmentId is Guid appointmentId && appointmentId != Guid.Empty)
        {
            var veterinarian = await unitOfWork.VeterinariansRepository.GetByIdAsync(order.VeterinarianId, cancellationToken);
            if (veterinarian is not null)
            {
                var message = $"Se ha completado la orden de procedimiento para la cita.";
                await sender.Send(
                    new CreateNotificationCommand(
                        veterinarian.UserId,
                        appointmentId,
                        message,
                        DateTime.UtcNow,
                        "Pendiente",
                        "ProcedimientoComp"),
                    cancellationToken);
            }
        }

        return Unit.Value;
    }
}

public sealed class GetProcedureOrderByIdQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetProcedureOrderByIdQuery, ProcedureOrder>
{
    public async Task<ProcedureOrder> Handle(
        GetProcedureOrderByIdQuery request,
        CancellationToken cancellationToken)
    {
        var order = await unitOfWork.ProcedureOrdersRepository.GetByIdAsync(request.Id, cancellationToken);
        if (order is null)
        {
            throw new NotFoundException($"No se encontró la orden de procedimiento con ID '{request.Id}'.");
        }

        return order;
    }
}

public sealed class GetProcedureOrdersByAppointmentIdQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetProcedureOrdersByAppointmentIdQuery, IEnumerable<ProcedureOrder>>
{
    public async Task<IEnumerable<ProcedureOrder>> Handle(
        GetProcedureOrdersByAppointmentIdQuery request,
        CancellationToken cancellationToken)
    {
        return await unitOfWork.ProcedureOrdersRepository.GetByAppointmentIdAsync(request.AppointmentId, cancellationToken);
    }
}

public sealed class GetPendingProcedureOrdersQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetPendingProcedureOrdersQuery, IEnumerable<ProcedureOrder>>
{
    public async Task<IEnumerable<ProcedureOrder>> Handle(
        GetPendingProcedureOrdersQuery request,
        CancellationToken cancellationToken)
    {
        return await unitOfWork.ProcedureOrdersRepository.GetPendingAsync(cancellationToken);
    }
}

// Validator
public sealed class CreateProcedureOrderCommandValidator : AbstractValidator<CreateProcedureOrderCommand>
{
    public CreateProcedureOrderCommandValidator()
    {
        RuleFor(x => x.ClientPetId)
            .NotEmpty().WithMessage("El paciente es obligatorio.");

        RuleFor(x => x)
            .Must(x => (x.AppointmentId.HasValue && x.AppointmentId.Value != Guid.Empty) ^ (x.HospitalizationStayId.HasValue && x.HospitalizationStayId.Value != Guid.Empty))
            .WithMessage("Debe especificar exactamente uno de los orígenes: AppointmentId o HospitalizationStayId.");

        When(x => x.IsInHouse, () =>
        {
            RuleFor(x => x.Items)
                .NotNull().WithMessage("Debe incluir al menos un ítem de procedimiento en una orden interna.")
                .Must(items => items != null && items.Count > 0)
                .WithMessage("Debe incluir al menos un ítem de procedimiento en una orden interna.");

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

