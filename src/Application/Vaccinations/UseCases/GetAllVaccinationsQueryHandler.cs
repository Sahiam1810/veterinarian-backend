using Application.Common.Abstractions;
using Domain.Vaccinations.Entities;
using MediatR;

namespace Application.Vaccinations.UseCases;

public sealed class GetAllVaccinationsQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetAllVaccinationsQuery, IReadOnlyCollection<Vaccination>>
{
    public async Task<IReadOnlyCollection<Vaccination>> Handle(
        GetAllVaccinationsQuery request,
        CancellationToken cancellationToken)
        => await unitOfWork.VaccinationsRepository.GetAllAsync(cancellationToken);
}
