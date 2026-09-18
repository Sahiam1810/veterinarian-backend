namespace Api.Reports.Dtos;

public sealed record AppointmentStatusReportResponseDto(
    Guid StatusId,
    string StatusName,
    int Count,
    double Percentage);
