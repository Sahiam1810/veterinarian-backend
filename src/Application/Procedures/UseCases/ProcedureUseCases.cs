using Application.Common.Abstractions;
using Application.Common.Exceptions;
using Application.Procedures.Abstraction;
using Domain.Procedures.Entities;
using FluentValidation;
using MediatR;

namespace Application.Procedures.UseCases;

// Queries
public sealed record GetAllProceduresQuery(bool OnlyActive = true) : IRequest<IEnumerable<Procedure>>;

public sealed record GetProcedureByIdQuery(Guid Id) : IRequest<Procedure>;

// Commands
public sealed record CreateProcedureCommand(string Name, string? Code, bool IsActive = true) : IRequest<Procedure>;

public sealed record UpdateProcedureCommand(Guid Id, string Name, string? Code, bool IsActive) : IRequest<Unit>;

public sealed record DeleteProcedureCommand(Guid Id) : IRequest<Unit>;

// Query Handlers
public sealed class GetAllProceduresQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetAllProceduresQuery, IEnumerable<Procedure>>
{
    public async Task<IEnumerable<Procedure>> Handle(
        GetAllProceduresQuery request,
        CancellationToken cancellationToken)
    {
        return await unitOfWork.ProceduresRepository.GetAllAsync(request.OnlyActive, cancellationToken);
    }
}

public sealed class GetProcedureByIdQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetProcedureByIdQuery, Procedure>
{
    public async Task<Procedure> Handle(
        GetProcedureByIdQuery request,
        CancellationToken cancellationToken)
    {
        var procedure = await unitOfWork.ProceduresRepository.GetByIdAsync(request.Id, cancellationToken);
        if (procedure is null)
        {
            throw new NotFoundException($"No se encontró el procedimiento con ID '{request.Id}'.");
        }

        return procedure;
    }
}

// Command Handlers
public sealed class CreateProcedureCommandHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<CreateProcedureCommand, Procedure>
{
    public async Task<Procedure> Handle(
        CreateProcedureCommand request,
        CancellationToken cancellationToken)
    {
        var procedure = new Procedure(request.Name, request.Code, request.IsActive);
        await unitOfWork.ProceduresRepository.AddAsync(procedure, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return procedure;
    }
}

public sealed class UpdateProcedureCommandHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateProcedureCommand, Unit>
{
    public async Task<Unit> Handle(
        UpdateProcedureCommand request,
        CancellationToken cancellationToken)
    {
        var procedure = await unitOfWork.ProceduresRepository.GetByIdAsync(request.Id, cancellationToken);
        if (procedure is null)
        {
            throw new NotFoundException($"No se encontró el procedimiento con ID '{request.Id}'.");
        }

        procedure.Update(request.Name, request.Code, request.IsActive);
        await unitOfWork.ProceduresRepository.UpdateAsync(procedure, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}

public sealed class DeleteProcedureCommandHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<DeleteProcedureCommand, Unit>
{
    public async Task<Unit> Handle(
        DeleteProcedureCommand request,
        CancellationToken cancellationToken)
    {
        var procedure = await unitOfWork.ProceduresRepository.GetByIdAsync(request.Id, cancellationToken);
        if (procedure is null)
        {
            throw new NotFoundException($"No se encontró el procedimiento con ID '{request.Id}'.");
        }

        await unitOfWork.ProceduresRepository.DeleteAsync(request.Id, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}

// Validators
public sealed class CreateProcedureCommandValidator : AbstractValidator<CreateProcedureCommand>
{
    public CreateProcedureCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("El nombre del procedimiento es obligatorio.")
            .MaximumLength(150).WithMessage("El nombre del procedimiento no puede superar los 150 caracteres.");

        RuleFor(x => x.Code)
            .MaximumLength(50).WithMessage("El código no puede superar los 50 caracteres.");
    }
}

public sealed class UpdateProcedureCommandValidator : AbstractValidator<UpdateProcedureCommand>
{
    public UpdateProcedureCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("El identificador del procedimiento es obligatorio.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("El nombre del procedimiento es obligatorio.")
            .MaximumLength(150).WithMessage("El nombre del procedimiento no puede superar los 150 caracteres.");

        RuleFor(x => x.Code)
            .MaximumLength(50).WithMessage("El código no puede superar los 50 caracteres.");
    }
}
