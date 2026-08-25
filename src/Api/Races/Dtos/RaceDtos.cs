using System.ComponentModel.DataAnnotations;

namespace Api.Races.Dtos;

public record CreateRaceDto(
    [Required(ErrorMessage = "El nombre de la raza es obligatorio.")]
    [MaxLength(20, ErrorMessage = "El nombre no puede superar los 20 caracteres.")]
    string Name
);

public record UpdateRaceDto(
    [Required(ErrorMessage = "El nombre de la raza es obligatorio.")]
    [MaxLength(20, ErrorMessage = "El nombre no puede superar los 20 caracteres.")]
    string Name
);

public record RaceResponseDto(
    Guid Id,
    string Name
);