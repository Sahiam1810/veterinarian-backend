using Application.Common.Abstractions;
using MediatR;

namespace Application.Veterinarians.UseCases;

public sealed class DeleteVeterinarianCommandHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<DeleteVeterinarianCommand, bool>
{
    public async Task<bool> Handle(
        DeleteVeterinarianCommand request,
        CancellationToken cancellationToken)
    {
        var veterinarian = await unitOfWork.VeterinariansRepository.GetByIdAsync(
            request.Id,
            cancellationToken);

        if (veterinarian is null)
        {
            return false;
        }

        await unitOfWork.VeterinariansRepository.DeleteAsync(
            veterinarian,
            cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }
}
