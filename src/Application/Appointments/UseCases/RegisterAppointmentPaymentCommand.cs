using MediatR;

namespace Application.Appointments.UseCases;

public sealed record RegisterAppointmentPaymentCommand(Guid AppointmentId) : IRequest;
