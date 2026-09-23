using Domain.Common;

namespace Domain.HospitalizationStays.Entities;

public enum HospitalizationStayStatus
{
    Activa,
    DadaDeAlta
}

public sealed class HospitalizationStay : BaseEntity<Guid>
{
    private HospitalizationStay()
    {
    }

    public HospitalizationStay(
        Guid clientPetId,
        Guid? appointmentId,
        Guid admittedByUserId,
        string motivo)
    {
        if (clientPetId == Guid.Empty)
        {
            throw new ArgumentException("El paciente es obligatorio.", nameof(clientPetId));
        }

        if (admittedByUserId == Guid.Empty)
        {
            throw new ArgumentException("El usuario que admite es obligatorio.", nameof(admittedByUserId));
        }

        if (string.IsNullOrWhiteSpace(motivo))
        {
            throw new ArgumentException("El motivo es obligatorio.", nameof(motivo));
        }

        Id = Guid.NewGuid();
        ClientPetId = clientPetId;
        AppointmentId = appointmentId;
        AdmittedByUserId = admittedByUserId;
        Motivo = motivo.Trim();
        FechaIngreso = DateTime.UtcNow;
        Estado = HospitalizationStayStatus.Activa;
    }

    public Guid ClientPetId { get; private set; }
    public Guid? AppointmentId { get; private set; }
    public Guid AdmittedByUserId { get; private set; }
    public DateTime FechaIngreso { get; private set; }
    public DateTime? FechaAlta { get; private set; }
    public HospitalizationStayStatus Estado { get; private set; }
    public string Motivo { get; private set; } = string.Empty;

    public void Discharge()
    {
        if (Estado == HospitalizationStayStatus.DadaDeAlta)
        {
            throw new InvalidOperationException("La estancia ya está dada de alta.");
        }

        Estado = HospitalizationStayStatus.DadaDeAlta;
        FechaAlta = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }
}
