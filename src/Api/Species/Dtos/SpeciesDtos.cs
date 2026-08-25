using System.ComponentModel.DataAnnotations;

namespace Api.Species.Dtos;

public record CreateSpeciesDto(
    [Required(ErrorMessage = "El nombre de la especie es obligatorio.")]
    [MaxLength(20, ErrorMessage = "El nombre no puede superar los 20 caracteres.")]
    string Name
);

public record UpdateSpeciesDto(
    [Required(ErrorMessage = "El nombre de la especie es obligatorio.")]
    [MaxLength(20, ErrorMessage = "El nombre no puede superar los 20 caracteres.")]
    string Name
);

public record SpeciesResponseDto(
    Guid Id,
    string Name
);
