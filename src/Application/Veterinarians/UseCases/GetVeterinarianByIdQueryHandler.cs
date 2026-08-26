using Application.Common.Abstractions;
using Domain.Veterinarians.Entities;
using MediatR;

namespace Application.Veterinarians.UseCases;

public sealed class GetVeterinarianByIdQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetVeterinarianByIdQuery, Veterinarian?>
{
    public Task<Veterinarian?> Handle(
        GetVeterinarianByIdQuery request,
        CancellationToken cancellationToken)
    {
        return unitOfWork.VeterinariansRepository.GetByIdAsync(
            request.Id,
            cancellationToken);
    }
}
