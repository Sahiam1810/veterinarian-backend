using Application.Common.Abstractions;
using MediatR;

namespace Application.TypeServices.UseCases;

public sealed class UpdateTypeServiceCommandHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateTypeServiceCommand, bool>
{
    public async Task<bool> Handle(
        UpdateTypeServiceCommand request,
        CancellationToken cancellationToken)
    {
        var typeService = await unitOfWork.TypeServicesRepository.GetByIdAsync(
            request.Id,
            cancellationToken);

        if (typeService is null)
        {
            return false;
        }

        typeService.Update(
            request.Name,
            request.Description);

        await unitOfWork.TypeServicesRepository.UpdateAsync(
            typeService,
            cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }
}
