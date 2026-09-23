using Application.Common.Abstractions;
using Application.Common.Exceptions;
using Application.Supplies.Abstraction;
using Domain.Supplies.Entities;
using FluentValidation;
using MediatR;

namespace Application.Supplies.UseCases;

// Queries
public sealed record GetAllSuppliesQuery(bool OnlyActive = true) : IRequest<IEnumerable<Supply>>;

public sealed record GetSupplyByIdQuery(Guid Id) : IRequest<Supply>;

// Commands
public sealed record CreateSupplyCommand(string Name, string Unit, decimal UnitPrice, decimal Stock, bool IsActive = true) : IRequest<Supply>;

public sealed record UpdateSupplyCommand(Guid Id, string Name, string Unit, decimal UnitPrice, decimal Stock, bool IsActive) : IRequest<Unit>;

public sealed record DeleteSupplyCommand(Guid Id) : IRequest<Unit>;

// Query Handlers
public sealed class GetAllSuppliesQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetAllSuppliesQuery, IEnumerable<Supply>>
{
    public async Task<IEnumerable<Supply>> Handle(
        GetAllSuppliesQuery request,
        CancellationToken cancellationToken)
    {
        return await unitOfWork.SuppliesRepository.GetAllAsync(request.OnlyActive, cancellationToken);
    }
}

public sealed class GetSupplyByIdQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetSupplyByIdQuery, Supply>
{
    public async Task<Supply> Handle(
        GetSupplyByIdQuery request,
        CancellationToken cancellationToken)
    {
        var supply = await unitOfWork.SuppliesRepository.GetByIdAsync(request.Id, cancellationToken);
        if (supply is null)
        {
            throw new NotFoundException($"No se encontró el insumo con ID '{request.Id}'.");
        }

        return supply;
    }
}

// Command Handlers
public sealed class CreateSupplyCommandHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<CreateSupplyCommand, Supply>
{
    public async Task<Supply> Handle(
        CreateSupplyCommand request,
        CancellationToken cancellationToken)
    {
        var supply = new Supply(request.Name, request.Unit, request.UnitPrice, request.Stock, request.IsActive);
        await unitOfWork.SuppliesRepository.AddAsync(supply, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return supply;
    }
}

public sealed class UpdateSupplyCommandHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateSupplyCommand, Unit>
{
    public async Task<Unit> Handle(
        UpdateSupplyCommand request,
        CancellationToken cancellationToken)
    {
        var supply = await unitOfWork.SuppliesRepository.GetByIdAsync(request.Id, cancellationToken);
        if (supply is null)
        {
            throw new NotFoundException($"No se encontró el insumo con ID '{request.Id}'.");
        }

        supply.Update(request.Name, request.Unit, request.UnitPrice, request.Stock, request.IsActive);
        await unitOfWork.SuppliesRepository.UpdateAsync(supply, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}

public sealed class DeleteSupplyCommandHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<DeleteSupplyCommand, Unit>
{
    public async Task<Unit> Handle(
        DeleteSupplyCommand request,
        CancellationToken cancellationToken)
    {
        var supply = await unitOfWork.SuppliesRepository.GetByIdAsync(request.Id, cancellationToken);
        if (supply is null)
        {
            throw new NotFoundException($"No se encontró el insumo con ID '{request.Id}'.");
        }

        await unitOfWork.SuppliesRepository.DeleteAsync(request.Id, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}

// Validators
public sealed class CreateSupplyCommandValidator : AbstractValidator<CreateSupplyCommand>
{
    public CreateSupplyCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("El nombre del insumo es obligatorio.")
            .MaximumLength(150).WithMessage("El nombre del insumo no puede superar los 150 caracteres.");

        RuleFor(x => x.Unit)
            .NotEmpty().WithMessage("La unidad del insumo es obligatoria.")
            .MaximumLength(50).WithMessage("La unidad no puede superar los 50 caracteres.");

        RuleFor(x => x.UnitPrice)
            .GreaterThanOrEqualTo(0).WithMessage("El precio unitario no puede ser negativo.");

        RuleFor(x => x.Stock)
            .GreaterThanOrEqualTo(0).WithMessage("El stock inicial no puede ser negativo.");
    }
}

public sealed class UpdateSupplyCommandValidator : AbstractValidator<UpdateSupplyCommand>
{
    public UpdateSupplyCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("El identificador del insumo es obligatorio.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("El nombre del insumo es obligatorio.")
            .MaximumLength(150).WithMessage("El nombre del insumo no puede superar los 150 caracteres.");

        RuleFor(x => x.Unit)
            .NotEmpty().WithMessage("La unidad del insumo es obligatoria.")
            .MaximumLength(50).WithMessage("La unidad no puede superar los 50 caracteres.");

        RuleFor(x => x.UnitPrice)
            .GreaterThanOrEqualTo(0).WithMessage("El precio unitario no puede ser negativo.");

        RuleFor(x => x.Stock)
            .GreaterThanOrEqualTo(0).WithMessage("El stock no puede ser negativo.");
    }
}
