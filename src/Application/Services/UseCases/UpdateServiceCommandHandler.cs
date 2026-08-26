using Application.Common.Abstractions;
using MediatR;

namespace Application.Services.UseCases;

public sealed class UpdateServiceCommandHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateServiceCommand, bool>
{
    public async Task<bool> Handle(
        UpdateServiceCommand request,
        CancellationToken cancellationToken)
    {
        var service = await unitOfWork.ServicesRepository.GetByIdAsync(
            request.Id,
            cancellationToken);

        if (service is null)
        {
            return false;
        }

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

        return true;
    }
}
