using System.ComponentModel.DataAnnotations;

namespace Api.Pets.Dtos;

public record CreatePetDto(
    [Required(ErrorMessage = "El nombre de la mascota es obligatorio.")]
    [MaxLength(50, ErrorMessage = "El nombre no puede superar los 50 caracteres.")]
    string Name,

    [Range(0, 150, ErrorMessage = "La edad debe estar entre 0 y 150 años.")]
    int Age,

    [Required(ErrorMessage = "El género es obligatorio. Use 'M' o 'F'.")]
    [MaxLength(1, ErrorMessage = "El género debe ser un solo carácter.")]
    string Gender,

    [Range(0.01, 500, ErrorMessage = "El peso debe estar entre 0.01 y 500 kg.")]
    decimal Weight,

    [MaxLength(500, ErrorMessage = "Las observaciones no pueden superar los 500 caracteres.")]
    string? Observations,

    [Required(ErrorMessage = "El ID de la especie es obligatorio.")]
    Guid SpeciesId,

    [Required(ErrorMessage = "El ID de la raza es obligatorio.")]
    Guid RaceId
);

public record UpdatePetDto(
    [Required(ErrorMessage = "El nombre de la mascota es obligatorio.")]
    [MaxLength(50, ErrorMessage = "El nombre no puede superar los 50 caracteres.")]
    string Name,

    [Range(0, 150, ErrorMessage = "La edad debe estar entre 0 y 150 años.")]
    int Age,

    [Required(ErrorMessage = "El género es obligatorio. Use 'M' o 'F'.")]
    [MaxLength(1, ErrorMessage = "El género debe ser un solo carácter.")]
    string Gender,

    [Range(0.01, 500, ErrorMessage = "El peso debe estar entre 0.01 y 500 kg.")]
    decimal Weight,

    [MaxLength(500, ErrorMessage = "Las observaciones no pueden superar los 500 caracteres.")]
    string? Observations,

    [Required(ErrorMessage = "El ID de la especie es obligatorio.")]
    Guid SpeciesId,

    [Required(ErrorMessage = "El ID de la raza es obligatorio.")]
    Guid RaceId
);

public record PetResponseDto(
    Guid Id,
    string Name,
    int Age,
    string Gender,
    decimal Weight,
    string? Observations,
    Guid SpeciesId,
    Guid RaceId
);
