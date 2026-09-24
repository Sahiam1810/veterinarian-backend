using Application.Common.Abstractions;
using Application.Common.Exceptions;
using Application.Medications.Abstraction;
using Domain.Medications.Entities;
using FluentValidation;
using MediatR;

namespace Application.Medications.UseCases;

// Queries
public sealed record GetAllMedicationsQuery(bool OnlyActive = true) : IRequest<IEnumerable<Medication>>;

public sealed record GetMedicationByIdQuery(Guid Id) : IRequest<Medication>;

// Commands
public sealed record CreateMedicationCommand(string Name, string? Code, bool IsActive = true, decimal Price = 0m) : IRequest<Medication>;

public sealed record UpdateMedicationCommand(Guid Id, string Name, string? Code, bool IsActive, decimal Price = 0m) : IRequest<Unit>;

public sealed record DeleteMedicationCommand(Guid Id) : IRequest<Unit>;

// Query Handlers
public sealed class GetAllMedicationsQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetAllMedicationsQuery, IEnumerable<Medication>>
{
    public async Task<IEnumerable<Medication>> Handle(
        GetAllMedicationsQuery request,
        CancellationToken cancellationToken)
    {
        return await unitOfWork.MedicationsRepository.GetAllAsync(request.OnlyActive, cancellationToken);
    }
}

public sealed class GetMedicationByIdQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetMedicationByIdQuery, Medication>
{
    public async Task<Medication> Handle(
        GetMedicationByIdQuery request,
        CancellationToken cancellationToken)
    {
        var medication = await unitOfWork.MedicationsRepository.GetByIdAsync(request.Id, cancellationToken);
        if (medication is null)
        {
            throw new NotFoundException($"No se encontró el medicamento con ID '{request.Id}'.");
        }

        return medication;
    }
}

// Command Handlers
public sealed class CreateMedicationCommandHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<CreateMedicationCommand, Medication>
{
    public async Task<Medication> Handle(
        CreateMedicationCommand request,
        CancellationToken cancellationToken)
    {

        var medication = new Medication(request.Name, request.Code, request.IsActive, request.Price);

        if (await unitOfWork.MedicationsRepository.ExistsByCodeAsync(request.Code, cancellationToken))
        {
            throw new ConflictException(
                $"Ya existe un medicamento registrado con el código '{Medication.NormalizeCode(request.Code)}'.",
                "Medications.CodeAlreadyExists");
        }

        var medication = new Medication(request.Name, request.Code, request.IsActive);

        await unitOfWork.MedicationsRepository.AddAsync(medication, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return medication;
    }
}

public sealed class UpdateMedicationCommandHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateMedicationCommand, Unit>
{
    public async Task<Unit> Handle(
        UpdateMedicationCommand request,
        CancellationToken cancellationToken)
    {
        var medication = await unitOfWork.MedicationsRepository.GetByIdAsync(request.Id, cancellationToken);
        if (medication is null)
        {
            throw new NotFoundException($"No se encontró el medicamento con ID '{request.Id}'.");
        }


        medication.Update(request.Name, request.Code, request.IsActive, request.Price);

        if (await unitOfWork.MedicationsRepository.ExistsByCodeAsync(
                request.Code,
                cancellationToken,
                request.Id))
        {
            throw new ConflictException(
                $"Ya existe un medicamento registrado con el código '{Medication.NormalizeCode(request.Code)}'.",
                "Medications.CodeAlreadyExists");
        }

        medication.Update(request.Name, request.Code, request.IsActive);

        await unitOfWork.MedicationsRepository.UpdateAsync(medication, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}

public sealed class DeleteMedicationCommandHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<DeleteMedicationCommand, Unit>
{
    public async Task<Unit> Handle(
        DeleteMedicationCommand request,
        CancellationToken cancellationToken)
    {
        var medication = await unitOfWork.MedicationsRepository.GetByIdAsync(request.Id, cancellationToken);
        if (medication is null)
        {
            throw new NotFoundException($"No se encontró el medicamento con ID '{request.Id}'.");
        }

        await unitOfWork.MedicationsRepository.DeleteAsync(request.Id, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}

// Validators
public sealed class CreateMedicationCommandValidator : AbstractValidator<CreateMedicationCommand>
{
    public CreateMedicationCommandValidator()
    {
        RuleFor(x => x.Price)
            .GreaterThanOrEqualTo(0m).WithMessage("El precio no puede ser negativo.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("El nombre del medicamento es obligatorio.")
            .MaximumLength(150).WithMessage("El nombre del medicamento no puede superar los 150 caracteres.");

        RuleFor(x => x.Code)
            .MaximumLength(50).WithMessage("El código no puede superar los 50 caracteres.");
    }
}

public sealed class UpdateMedicationCommandValidator : AbstractValidator<UpdateMedicationCommand>
{
    public UpdateMedicationCommandValidator()
    {
        RuleFor(x => x.Price)
            .GreaterThanOrEqualTo(0m).WithMessage("El precio no puede ser negativo.");

        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("El identificador del medicamento es obligatorio.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("El nombre del medicamento es obligatorio.")
            .MaximumLength(150).WithMessage("El nombre del medicamento no puede superar los 150 caracteres.");

        RuleFor(x => x.Code)
            .MaximumLength(50).WithMessage("El código no puede superar los 50 caracteres.");
    }
}
