using Domain.Appointments.Entities;
using Domain.ClientsPets.Entities;
using Domain.Common;
using Domain.StatusAppointments.Entities;

namespace Domain.AppointmentStatusHistories.Entities;

public sealed class AppointmentStatusHistory : BaseEntity<Guid>
{
    private AppointmentStatusHistory()
    {
    }

    public AppointmentStatusHistory(
        Guid appointmentId,
        Guid statusId,
        Guid clientPetId,
        string? comment)
    {
        Id = Guid.NewGuid();
        AppointmentId = appointmentId;
        StatusId = statusId;
        ClientPetId = clientPetId;
        Comment = comment;
    }

    public Guid AppointmentId { get; private set; }
    public Appointment? Appointment { get; private set; }

    public Guid StatusId { get; private set; }
    public StatusAppointment? Status { get; private set; }

    public Guid ClientPetId { get; private set; }
    public ClientPetEntity? ClientPet { get; private set; }

    public string? Comment { get; private set; }

    public void Update(
        Guid appointmentId,
        Guid statusId,
        Guid clientPetId,
        string? comment)
    {
        AppointmentId = appointmentId;
        StatusId = statusId;
        ClientPetId = clientPetId;
        Comment = comment;
        UpdatedAt = DateTime.UtcNow;
    }
}
