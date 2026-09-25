using System.ComponentModel.DataAnnotations;

namespace Api.Appointments.Dtos;

public sealed record QuickBookingAppointmentRequest(
    Guid? ClientId,
    string? ClientPhoneNumber,
    string? ClientFullName,

    [Required(ErrorMessage = "El nombre de la mascota es obligatorio.")]
    [MaxLength(50, ErrorMessage = "El nombre de la mascota no puede superar los 50 caracteres.")]
    string PetName,

    [Required(ErrorMessage = "El ID de la especie es obligatorio.")]
    Guid SpeciesId,

    [Required(ErrorMessage = "El ID del servicio es obligatorio.")]
    Guid ServiceId,

    [Required(ErrorMessage = "El ID del veterinario es obligatorio.")]
    Guid VeterinarianId,

    [Required(ErrorMessage = "La fecha y hora de inicio son obligatorias.")]
    DateTime ScheduledStart,

    DateTime? ScheduledEnd = null,
    Guid? AvailabilityId = null,
    string? ConsultingRoom = null,
    string? Notes = null
);
