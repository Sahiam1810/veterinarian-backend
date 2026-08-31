using Application.Common.Abstractions;
using Application.Common.Exceptions;
using Domain.Modules.Entities;
using MediatR;

namespace Application.Modules.UseCases;

public sealed record CreateModuleCommand(string Name, string? Description) : IRequest<Guid>;

public sealed record GetAllModulesQuery : IRequest<IReadOnlyCollection<ModuleEntity>>;

public sealed record GetModuleByIdQuery(Guid Id) : IRequest<ModuleEntity>;

public sealed record UpdateModuleCommand(Guid Id, string Name, string? Description) : IRequest;

public sealed record DeleteModuleCommand(Guid Id) : IRequest;

// Crea un módulo de la aplicación.
public sealed class CreateModuleCommandHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<CreateModuleCommand, Guid>
{
    public async Task<Guid> Handle(CreateModuleCommand request, CancellationToken cancellationToken)
    {
        if (await unitOfWork.ModulesRepository.ExistsByNameAsync(request.Name, cancellationToken))
        {
            throw new ConflictException("Ya existe un módulo con ese nombre.");
        }

        var module = new ModuleEntity(request.Name, request.Description);
        await unitOfWork.ModulesRepository.AddAsync(module, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return module.Id;
    }
}

// Lista todos los módulos.
public sealed class GetAllModulesQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetAllModulesQuery, IReadOnlyCollection<ModuleEntity>>
{
    public Task<IReadOnlyCollection<ModuleEntity>> Handle(
        GetAllModulesQuery request,
        CancellationToken cancellationToken) =>
        unitOfWork.ModulesRepository.GetAllAsync(cancellationToken);
}

// Obtiene un módulo por id.
public sealed class GetModuleByIdQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetModuleByIdQuery, ModuleEntity>
{
    public async Task<ModuleEntity> Handle(GetModuleByIdQuery request, CancellationToken cancellationToken) =>
        await unitOfWork.ModulesRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException("Módulo no encontrado.");
}

// Actualiza un módulo.
public sealed class UpdateModuleCommandHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateModuleCommand>
{
    public async Task Handle(UpdateModuleCommand request, CancellationToken cancellationToken)
    {
        var module = await unitOfWork.ModulesRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException("Módulo no encontrado.");

        var existing = await unitOfWork.ModulesRepository.GetByNameAsync(request.Name, cancellationToken);
        if (existing is not null && existing.Id != module.Id)
        {
            throw new ConflictException("Ya existe un módulo con ese nombre.");
        }

        module.Update(request.Name, request.Description);
        await unitOfWork.ModulesRepository.UpdateAsync(module, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}

// Elimina un módulo.
public sealed class DeleteModuleCommandHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<DeleteModuleCommand>
{
    public async Task Handle(DeleteModuleCommand request, CancellationToken cancellationToken)
    {
        var module = await unitOfWork.ModulesRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException("Módulo no encontrado.");

        await unitOfWork.ModulesRepository.DeleteAsync(module, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
