namespace Api.Medications.Dtos;

public record MedicationDto(
    Guid Id,
    string Name,
    string? Code,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

public record CreateMedicationDto(
    string Name,
    string? Code,
    bool IsActive = true);

public record UpdateMedicationDto(
    string Name,
    string? Code,
    bool IsActive);
