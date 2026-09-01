using Application.Common.Abstractions;
using Application.Common.Exceptions;
using Domain.StatusAppointments.Entities;
using MediatR;

namespace Application.StatusAppointments.UseCases;

public sealed class GetStatusAppointmentByIdQueryHandler
    : IRequestHandler<GetStatusAppointmentByIdQuery, StatusAppointment>
{
    private readonly IUnitOfWork _uow;

    public GetStatusAppointmentByIdQueryHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<StatusAppointment> Handle(
        GetStatusAppointmentByIdQuery request,
        CancellationToken cancellationToken)
    {
        return await _uow.StatusAppointmentsRepository.GetByIdAsync(
            request.Id,
            cancellationToken)
            ?? throw new NotFoundException("Estado de cita no encontrado.");
    }
}
