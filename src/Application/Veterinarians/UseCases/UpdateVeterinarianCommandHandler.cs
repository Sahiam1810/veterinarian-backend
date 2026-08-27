using Application.Common.Abstractions;
using MediatR;

namespace Application.Veterinarians.UseCases;

public sealed class UpdateVeterinarianCommandHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateVeterinarianCommand, bool>
{
    public async Task<bool> Handle(
        UpdateVeterinarianCommand request,
        CancellationToken cancellationToken)
    {
        var veterinarian = await unitOfWork.VeterinariansRepository.GetByIdAsync(
            request.Id,
            cancellationToken);

        if (veterinarian is null)
        {
            return false;
        }

        veterinarian.Update(
            request.UserId,
            request.SpecialtyId,
            request.LicenseNumber);

        await unitOfWork.VeterinariansRepository.UpdateAsync(
            veterinarian,
            cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }
}
