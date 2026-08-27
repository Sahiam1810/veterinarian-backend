using Application.Common.Abstractions;
using Domain.Vaccinations.Entities;
using MediatR;

namespace Application.Vaccinations.UseCases;

public sealed class GetAllVaccinationsQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetAllVaccinationsQuery, IReadOnlyCollection<Vaccination>>
{
    public Task<IReadOnlyCollection<Vaccination>> Handle(
        GetAllVaccinationsQuery request,
        CancellationToken cancellationToken)
    {
        return unitOfWork.VaccinationsRepository.GetAllAsync(cancellationToken);
    }
}
