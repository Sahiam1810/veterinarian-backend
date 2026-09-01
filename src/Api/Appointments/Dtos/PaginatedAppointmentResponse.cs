using Application.Common.Models;

namespace Api.Appointments.Dtos;

public sealed record PaginatedAppointmentResponse(
    IReadOnlyCollection<AppointmentResponse> Items,
    PaginationMetadata Pagination);
