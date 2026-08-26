using Application.Common.Abstractions;
using MediatR;

namespace Application.TypeServices.UseCases;

public sealed class DeleteTypeServiceCommandHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<DeleteTypeServiceCommand, bool>
{
    public async Task<bool> Handle(
        DeleteTypeServiceCommand request,
        CancellationToken cancellationToken)
    {
        var typeService = await unitOfWork.TypeServicesRepository.GetByIdAsync(
            request.Id,
            cancellationToken);

        if (typeService is null)
        {
            return false;
        }

        await unitOfWork.TypeServicesRepository.DeleteAsync(
            typeService,
            cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }
}
