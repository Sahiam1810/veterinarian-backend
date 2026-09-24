using Application.Common.Abstractions;
using Application.Common.Exceptions;
using Application.Notifications.UseCases;
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
    Guid? HospitalizationStayId = null,
    Guid? RequestingUserId = null) : IRequest<ProcedureOrder>;

public sealed record CompleteProcedureOrderCommand(Guid Id, string? ResultFileUrl = null) : IRequest<Unit>;

public sealed record GetProcedureOrderByIdQuery(Guid Id) : IRequest<ProcedureOrder>;

public sealed record GetProcedureOrdersByAppointmentIdQuery(Guid AppointmentId) : IRequest<IEnumerable<ProcedureOrder>>;

public sealed record GetProcedureOrdersByHospitalizationStayIdQuery(Guid HospitalizationStayId) : IRequest<IEnumerable<ProcedureOrder>>;

public sealed record GetPendingProcedureOrdersQuery() : IRequest<IEnumerable<ProcedureOrder>>;

// Handlers
public sealed class CreateProcedureOrderCommandHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<CreateProcedureOrderCommand, ProcedureOrder>
{
    public async Task<ProcedureOrder> Handle(
        CreateProcedureOrderCommand request,
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
            ? request.Items.Select(item => (item.ProcedureId, item.Notes, UnitPrice: 0m)).ToList()
            : new List<(Guid ProcedureId, string? Notes, decimal UnitPrice)>();
        if (request.IsInHouse && request.Items is not null)
        {
            foreach (var item in request.Items)
            {
                var procedure = await unitOfWork.ProceduresRepository.GetByIdAsync(item.ProcedureId, cancellationToken)
                    ?? throw new NotFoundException($"No se encontró el procedimiento con ID '{item.ProcedureId}'.");

                if (!procedure.IsActive)
                {
                    throw new BadRequestException("No se puede ordenar un procedimiento inactivo.");
                }

                itemsTuple.Add((procedure.Id, item.Notes, procedure.Price));
            }
        }

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

        order.Complete(request.ResultFileUrl);
        await unitOfWork.ProcedureOrdersRepository.UpdateAsync(order, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        // Notificación al veterinario que realizó la orden
        var veterinarian = await unitOfWork.VeterinariansRepository.GetByIdAsync(order.VeterinarianId, cancellationToken);
        if (veterinarian is not null && order.AppointmentId is Guid appointmentId)
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

public sealed class GetProcedureOrdersByHospitalizationStayIdQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetProcedureOrdersByHospitalizationStayIdQuery, IEnumerable<ProcedureOrder>>
{
    public async Task<IEnumerable<ProcedureOrder>> Handle(
        GetProcedureOrdersByHospitalizationStayIdQuery request,
        CancellationToken cancellationToken)
        => await unitOfWork.ProcedureOrdersRepository.GetByHospitalizationStayIdAsync(
            request.HospitalizationStayId,
            cancellationToken);
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
            .Must(x => (x.AppointmentId is null) != (x.HospitalizationStayId is null))
            .WithMessage("Debe indicar exactamente una cita o una estancia de hospitalización.");

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
