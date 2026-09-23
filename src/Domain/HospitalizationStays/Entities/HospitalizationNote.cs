using Domain.Common;

namespace Domain.HospitalizationStays.Entities;

public sealed class HospitalizationNote : BaseEntity<Guid>
{
    private HospitalizationNote()
    {
    }

    public HospitalizationNote(
        Guid stayId,
        Guid autorUserId,
        string nota,
        Guid? entregadoAUserId)
    {
        if (stayId == Guid.Empty)
        {
            throw new ArgumentException("La estancia es obligatoria.", nameof(stayId));
        }

        if (autorUserId == Guid.Empty)
        {
            throw new ArgumentException("El autor es obligatorio.", nameof(autorUserId));
        }

        if (string.IsNullOrWhiteSpace(nota))
        {
            throw new ArgumentException("La nota es obligatoria.", nameof(nota));
        }

        Id = Guid.NewGuid();
        StayId = stayId;
        AutorUserId = autorUserId;
        Nota = nota.Trim();
        EntregadoAUserId = entregadoAUserId;
        FechaHora = DateTime.UtcNow;
    }

    public Guid StayId { get; private set; }
    public Guid AutorUserId { get; private set; }
    public DateTime FechaHora { get; private set; }
    public string Nota { get; private set; } = string.Empty;
    public Guid? EntregadoAUserId { get; private set; }
}
