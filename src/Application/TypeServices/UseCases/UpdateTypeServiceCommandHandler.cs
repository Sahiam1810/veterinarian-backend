using Application.Common.Abstractions;
using Application.Common.Exceptions;
using MediatR;

namespace Application.TypeServices.UseCases;

public sealed class UpdateTypeServiceCommandHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateTypeServiceCommand>
{
    public async Task Handle(
        UpdateTypeServiceCommand request,
        CancellationToken cancellationToken)
    {
        var typeService = await unitOfWork.TypeServicesRepository.GetByIdAsync(
            request.Id,
            cancellationToken)
            ?? throw new NotFoundException("Tipo de servicio no encontrado.");

        typeService.Update(
            request.Name,
            request.Description);

        await unitOfWork.TypeServicesRepository.UpdateAsync(
            typeService,
            cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
