using Application.Common.Abstractions;
using Application.Common.Exceptions;
using MediatR;

namespace Application.Services.UseCases;

public sealed class DeleteServiceCommandHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<DeleteServiceCommand>
{
    public async Task Handle(
        DeleteServiceCommand request,
        CancellationToken cancellationToken)
    {
        var service = await unitOfWork.ServicesRepository.GetByIdAsync(
            request.Id,
            cancellationToken)
            ?? throw new NotFoundException("Servicio no encontrado.");

        await unitOfWork.ServicesRepository.DeleteAsync(
            service,
            cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
