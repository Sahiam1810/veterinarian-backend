using Application.Common.Abstractions;
using Domain.Veterinarians.Entities;
using MediatR;

namespace Application.Veterinarians.UseCases;

public sealed class CreateVeterinarianCommandHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<CreateVeterinarianCommand, Guid>
{
    public async Task<Guid> Handle(
        CreateVeterinarianCommand request,
        CancellationToken cancellationToken)
    {
        var veterinarian = new Veterinarian(
            request.UserId,
            request.SpecialtyId,
            request.LicenseNumber);

        await unitOfWork.VeterinariansRepository.AddAsync(
            veterinarian,
            cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return veterinarian.Id;
    }
}
