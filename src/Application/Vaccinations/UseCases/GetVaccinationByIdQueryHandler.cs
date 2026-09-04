using Application.Common.Abstractions;
using Application.Common.Exceptions;
using Domain.Vaccinations.Entities;
using MediatR;

namespace Application.Vaccinations.UseCases;

public sealed class GetVaccinationByIdQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetVaccinationByIdQuery, Vaccination>
{
    public async Task<Vaccination> Handle(
        GetVaccinationByIdQuery request,
        CancellationToken cancellationToken)
    {
        return await unitOfWork.VaccinationsRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException("Registro de vacunación no encontrado.");
    }
}
