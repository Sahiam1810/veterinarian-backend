using Application.Common.Abstractions;
using Application.Common.Exceptions;
using Domain.MedicationOrders.Entities;
using FluentValidation;
using MediatR;

namespace Application.MedicationOrders.UseCases;

public sealed record MedicationOrderItemInput(Guid MedicationId, string? Notes);

public sealed record CreateMedicationOrderCommand(
    Guid ClientPetId,
    Guid AppointmentId,
    bool IsInHouse,
    string? ReferredTo,
    string? ReferralReason,
    List<MedicationOrderItemInput>? Items) : IRequest<MedicationOrder>;

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
        var appointment = await unitOfWork.AppointmentsRepository.GetByIdAsync(request.AppointmentId, cancellationToken);
        if (appointment is null)
        {
            throw new NotFoundException($"No se encontró la cita con ID '{request.AppointmentId}'.");
        }

        var itemsTuple = request.Items?.Select(i => (i.MedicationId, i.Notes));

        var medicationOrder = new MedicationOrder(
            request.ClientPetId,
            appointment.VeterinarianId,
            request.AppointmentId,
            request.IsInHouse,
            request.ReferredTo,
            request.ReferralReason,
            itemsTuple);

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

        RuleFor(x => x.AppointmentId)
            .NotEmpty().WithMessage("La consulta de origen es obligatoria.");

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
