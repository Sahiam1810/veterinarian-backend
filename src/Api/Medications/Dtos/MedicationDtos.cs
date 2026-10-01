namespace Api.Medications.Dtos;

public record MedicationDto(
    Guid Id,
    string Name,
    string? Code,
    bool IsActive,
    decimal Price,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

public record CreateMedicationDto(
    string Name,
    string? Code,
    bool IsActive = true,
    decimal Price = 0m);

public record UpdateMedicationDto(
    string Name,
    string? Code,
    bool IsActive,
    decimal Price = 0m);
