using Application.Common.Abstractions;
using MediatR;

namespace Application.Services.UseCases;

public sealed class DeleteServiceCommandHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<DeleteServiceCommand, bool>
{
    public async Task<bool> Handle(
        DeleteServiceCommand request,
        CancellationToken cancellationToken)
    {
        var service = await unitOfWork.ServicesRepository.GetByIdAsync(
            request.Id,
            cancellationToken);

        if (service is null)
        {
            return false;
        }

        await unitOfWork.ServicesRepository.DeleteAsync(
            service,
            cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }
}
