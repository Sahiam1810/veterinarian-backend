using Application.Common.Abstractions;
using Domain.StatusAppointments.Entities;
using MediatR;

namespace Application.StatusAppointments.UseCases;

public sealed class GetAllStatusAppointmentsQueryHandler
    : IRequestHandler<
        GetAllStatusAppointmentsQuery,
        IReadOnlyCollection<StatusAppointment>>
{
    private readonly IUnitOfWork _uow;

    public GetAllStatusAppointmentsQueryHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<IReadOnlyCollection<StatusAppointment>> Handle(
        GetAllStatusAppointmentsQuery request,
        CancellationToken cancellationToken)
    {
        return await _uow.StatusAppointmentsRepository.GetAllAsync(
            cancellationToken);
    }
}
