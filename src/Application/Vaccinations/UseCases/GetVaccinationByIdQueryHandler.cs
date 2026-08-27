using Application.Common.Abstractions;
using Domain.Vaccinations.Entities;
using MediatR;

namespace Application.Vaccinations.UseCases;

public sealed class GetVaccinationByIdQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetVaccinationByIdQuery, Vaccination?>
{
    public Task<Vaccination?> Handle(
        GetVaccinationByIdQuery request,
        CancellationToken cancellationToken)
    {
        return unitOfWork.VaccinationsRepository.GetByIdAsync(
            request.Id,
            cancellationToken);
    }
}
