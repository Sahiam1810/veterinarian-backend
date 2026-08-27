using Application.Common.Abstractions;
using Domain.Veterinarians.Entities;
using MediatR;

namespace Application.Veterinarians.UseCases;

public sealed class GetAllVeterinariansQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetAllVeterinariansQuery, IReadOnlyCollection<Veterinarian>>
{
    public Task<IReadOnlyCollection<Veterinarian>> Handle(
        GetAllVeterinariansQuery request,
        CancellationToken cancellationToken)
    {
        return unitOfWork.VeterinariansRepository.GetAllAsync(cancellationToken);
    }
}
