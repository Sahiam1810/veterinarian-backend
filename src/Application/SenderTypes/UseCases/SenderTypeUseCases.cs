using Application.Common.Abstractions;
using Application.Common.Exceptions;
using Domain.SenderTypes.Entities;
using MediatR;

namespace Application.SenderTypes.UseCases;

public sealed record CreateSenderTypeCommand(string Name) : IRequest<Guid>;
public sealed record GetAllSenderTypesQuery : IRequest<IReadOnlyCollection<SenderTypeEntity>>;
public sealed record GetSenderTypeByIdQuery(Guid Id) : IRequest<SenderTypeEntity>;
public sealed record UpdateSenderTypeCommand(Guid Id, string Name) : IRequest;
public sealed record DeleteSenderTypeCommand(Guid Id) : IRequest;

public sealed class CreateSenderTypeCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<CreateSenderTypeCommand, Guid>
{
    public async Task<Guid> Handle(CreateSenderTypeCommand request, CancellationToken cancellationToken)
    {
        var senderType = new SenderTypeEntity(request.Name);
        await unitOfWork.SenderTypesRepository.AddAsync(senderType, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return senderType.Id;
    }
}

public sealed class GetAllSenderTypesQueryHandler(IUnitOfWork unitOfWork) : IRequestHandler<GetAllSenderTypesQuery, IReadOnlyCollection<SenderTypeEntity>>
{
    public Task<IReadOnlyCollection<SenderTypeEntity>> Handle(GetAllSenderTypesQuery request, CancellationToken cancellationToken) => unitOfWork.SenderTypesRepository.GetAllAsync(cancellationToken);
}

public sealed class GetSenderTypeByIdQueryHandler(IUnitOfWork unitOfWork) : IRequestHandler<GetSenderTypeByIdQuery, SenderTypeEntity>
{
    public async Task<SenderTypeEntity> Handle(GetSenderTypeByIdQuery request, CancellationToken cancellationToken) =>
        await unitOfWork.SenderTypesRepository.GetByIdAsync(request.Id, cancellationToken) ?? throw new NotFoundException("Tipo de remitente no encontrado.");
}

public sealed class UpdateSenderTypeCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<UpdateSenderTypeCommand>
{
    public async Task Handle(UpdateSenderTypeCommand request, CancellationToken cancellationToken)
    {
        var senderType = await unitOfWork.SenderTypesRepository.GetByIdAsync(request.Id, cancellationToken) ?? throw new NotFoundException("Tipo de remitente no encontrado.");
        senderType.Update(request.Name);
        await unitOfWork.SenderTypesRepository.UpdateAsync(senderType, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}

public sealed class DeleteSenderTypeCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<DeleteSenderTypeCommand>
{
    public async Task Handle(DeleteSenderTypeCommand request, CancellationToken cancellationToken)
    {
        var senderType = await unitOfWork.SenderTypesRepository.GetByIdAsync(request.Id, cancellationToken) ?? throw new NotFoundException("Tipo de remitente no encontrado.");
        await unitOfWork.SenderTypesRepository.DeleteAsync(senderType, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
