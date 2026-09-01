using Application.Common.Abstractions;
using Application.Common.Exceptions;
using MediatR;

namespace Application.TypeServices.UseCases;

public sealed class DeleteTypeServiceCommandHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<DeleteTypeServiceCommand>
{
    public async Task Handle(
        DeleteTypeServiceCommand request,
        CancellationToken cancellationToken)
    {
        var typeService = await unitOfWork.TypeServicesRepository.GetByIdAsync(
            request.Id,
            cancellationToken)
            ?? throw new NotFoundException("Tipo de servicio no encontrado.");

        await unitOfWork.TypeServicesRepository.DeleteAsync(
            typeService,
            cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
