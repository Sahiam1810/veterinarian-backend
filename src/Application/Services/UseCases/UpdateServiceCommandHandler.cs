using Application.Common.Abstractions;
using Application.Common.Exceptions;
using MediatR;

namespace Application.Services.UseCases;

public sealed class UpdateServiceCommandHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateServiceCommand>
{
    public async Task Handle(
        UpdateServiceCommand request,
        CancellationToken cancellationToken)
    {
        var service = await unitOfWork.ServicesRepository.GetByIdAsync(
            request.Id,
            cancellationToken)
            ?? throw new NotFoundException("Servicio no encontrado.");

        service.Update(
            request.TypeServiceId,
            request.Name,
            request.DurationMinutes,
            request.Price,
            request.IsActive);

        await unitOfWork.ServicesRepository.UpdateAsync(
            service,
            cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
