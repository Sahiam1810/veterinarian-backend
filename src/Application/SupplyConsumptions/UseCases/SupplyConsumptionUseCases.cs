using Application.Common.Abstractions;
using Application.Common.Exceptions;
using Domain.HospitalizationStays.Entities;
using Domain.SupplyConsumptions.Entities;
using FluentValidation;
using MediatR;


namespace Application.SupplyConsumptions.UseCases;

// Queries
public sealed record GetSupplyConsumptionsByStayIdQuery(Guid HospitalizationStayId) : IRequest<IEnumerable<SupplyConsumption>>;

public sealed record GetSupplyConsumptionTotalByStayIdQuery(Guid HospitalizationStayId) : IRequest<decimal>;

// Commands
public sealed record RegisterSupplyConsumptionCommand(
    Guid HospitalizationStayId,
    Guid SupplyId,
    decimal Quantity,
    Guid RegisteredByUserId,
    string? Notes = null) : IRequest<SupplyConsumption>;

// Handlers
public sealed class GetSupplyConsumptionsByStayIdQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetSupplyConsumptionsByStayIdQuery, IEnumerable<SupplyConsumption>>
{
    public async Task<IEnumerable<SupplyConsumption>> Handle(
        GetSupplyConsumptionsByStayIdQuery request,
        CancellationToken cancellationToken)
    {
        return await unitOfWork.SupplyConsumptionsRepository.GetByHospitalizationStayIdAsync(request.HospitalizationStayId, cancellationToken);
    }
}

public sealed class GetSupplyConsumptionTotalByStayIdQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetSupplyConsumptionTotalByStayIdQuery, decimal>
{
    public async Task<decimal> Handle(
        GetSupplyConsumptionTotalByStayIdQuery request,
        CancellationToken cancellationToken)
    {
        return await unitOfWork.SupplyConsumptionsRepository.GetTotalByHospitalizationStayIdAsync(request.HospitalizationStayId, cancellationToken);
    }
}


public sealed class RegisterSupplyConsumptionCommandHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<RegisterSupplyConsumptionCommand, SupplyConsumption>
{
    public async Task<SupplyConsumption> Handle(
        RegisterSupplyConsumptionCommand request,
        CancellationToken cancellationToken)
    {
        var stay = await unitOfWork.HospitalizationStaysRepository.GetByIdAsync(request.HospitalizationStayId, cancellationToken);
        if (stay is null)
        {
            throw new NotFoundException($"No se encontró la estancia de hospitalización con ID '{request.HospitalizationStayId}'.");
        }

        if (stay.Estado != HospitalizationStayStatus.Activa)
        {
            throw new ConflictException("No se pueden registrar consumos de insumos en una estancia dada de alta.");
        }

        var supply = await unitOfWork.SuppliesRepository.GetByIdAsync(request.SupplyId, cancellationToken);
        if (supply is null)
        {
            throw new NotFoundException($"No se encontró el insumo con ID '{request.SupplyId}'.");
        }

        if (!supply.IsActive)
        {
            throw new ConflictException("El insumo seleccionado está inactivo y no se puede consumir.");
        }

        // Deducts stock (throws InvalidOperationException("Stock insuficiente.") if quantity > stock)
        supply.DeductStock(request.Quantity);

        var consumption = new SupplyConsumption(
            request.HospitalizationStayId,
            request.SupplyId,
            request.Quantity,
            supply.UnitPrice,
            request.RegisteredByUserId,
            request.Notes);

        await unitOfWork.SupplyConsumptionsRepository.AddAsync(consumption, cancellationToken);
        await unitOfWork.SuppliesRepository.UpdateAsync(supply, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return consumption;
    }
}


// Validators
public sealed class RegisterSupplyConsumptionCommandValidator : AbstractValidator<RegisterSupplyConsumptionCommand>
{
    public RegisterSupplyConsumptionCommandValidator()
    {
        RuleFor(x => x.HospitalizationStayId)
            .NotEmpty().WithMessage("El ID de la estancia de hospitalización es obligatorio.");

        RuleFor(x => x.SupplyId)
            .NotEmpty().WithMessage("El ID del insumo es obligatorio.");

        RuleFor(x => x.Quantity)
            .GreaterThan(0).WithMessage("La cantidad a consumir debe ser mayor a 0.");

        RuleFor(x => x.RegisteredByUserId)
            .NotEmpty().WithMessage("El ID del usuario que registra es obligatorio.");

        RuleFor(x => x.Notes)
            .MaximumLength(500).WithMessage("Las notas no pueden superar los 500 caracteres.");
    }
}
