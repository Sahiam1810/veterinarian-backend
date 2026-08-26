using Application.Common.Abstractions;
using Application.Common.Exceptions;
using Domain.Specialties.Entities;
using MediatR;

namespace Application.Specialties.UseCases;

public sealed record CreateSpecialtyCommand(string Name, string? Description) : IRequest<Guid>;
public sealed record GetAllSpecialtiesQuery : IRequest<IReadOnlyCollection<SpecialtyEntity>>;
public sealed record GetSpecialtyByIdQuery(Guid Id) : IRequest<SpecialtyEntity>;
public sealed record UpdateSpecialtyCommand(Guid Id, string Name, string? Description) : IRequest;
public sealed record DeleteSpecialtyCommand(Guid Id) : IRequest;

public sealed class CreateSpecialtyCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<CreateSpecialtyCommand, Guid>
{
    public async Task<Guid> Handle(CreateSpecialtyCommand request, CancellationToken cancellationToken)
    {
        if (await unitOfWork.SpecialtiesRepository.ExistsByNameAsync(request.Name.Trim(), cancellationToken))
            throw new ConflictException("Ya existe una especialidad con el mismo nombre.");
        var specialty = new SpecialtyEntity(request.Name, request.Description);
        await unitOfWork.SpecialtiesRepository.AddAsync(specialty, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return specialty.Id;
    }
}

public sealed class GetAllSpecialtiesQueryHandler(IUnitOfWork unitOfWork) : IRequestHandler<GetAllSpecialtiesQuery, IReadOnlyCollection<SpecialtyEntity>>
{
    public Task<IReadOnlyCollection<SpecialtyEntity>> Handle(GetAllSpecialtiesQuery request, CancellationToken cancellationToken) => unitOfWork.SpecialtiesRepository.GetAllAsync(cancellationToken);
}

public sealed class GetSpecialtyByIdQueryHandler(IUnitOfWork unitOfWork) : IRequestHandler<GetSpecialtyByIdQuery, SpecialtyEntity>
{
    public async Task<SpecialtyEntity> Handle(GetSpecialtyByIdQuery request, CancellationToken cancellationToken) =>
        await unitOfWork.SpecialtiesRepository.GetByIdAsync(request.Id, cancellationToken) ?? throw new NotFoundException("Especialidad no encontrada.");
}

public sealed class UpdateSpecialtyCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<UpdateSpecialtyCommand>
{
    public async Task Handle(UpdateSpecialtyCommand request, CancellationToken cancellationToken)
    {
        var specialty = await unitOfWork.SpecialtiesRepository.GetByIdAsync(request.Id, cancellationToken) ?? throw new NotFoundException("Especialidad no encontrada.");
        if (await unitOfWork.SpecialtiesRepository.ExistsByNameAsync(request.Name.Trim(), cancellationToken, request.Id))
            throw new ConflictException("Ya existe una especialidad con el mismo nombre.");
        specialty.Update(request.Name, request.Description);
        await unitOfWork.SpecialtiesRepository.UpdateAsync(specialty, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}

public sealed class DeleteSpecialtyCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<DeleteSpecialtyCommand>
{
    public async Task Handle(DeleteSpecialtyCommand request, CancellationToken cancellationToken)
    {
        var specialty = await unitOfWork.SpecialtiesRepository.GetByIdAsync(request.Id, cancellationToken) ?? throw new NotFoundException("Especialidad no encontrada.");
        await unitOfWork.SpecialtiesRepository.DeleteAsync(specialty, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
