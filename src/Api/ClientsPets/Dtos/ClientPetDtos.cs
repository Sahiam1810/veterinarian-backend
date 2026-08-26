using System.ComponentModel.DataAnnotations;

namespace Api.ClientsPets.Dtos;

public sealed record CreateClientPetDto(
    [Required(ErrorMessage = "El ID del cliente es obligatorio.")] Guid ClientId,
    [Required(ErrorMessage = "El ID de la mascota es obligatorio.")] Guid PetId,
    bool IsPrimaryOwner);

public sealed record UpdateClientPetDto(bool IsPrimaryOwner);

public sealed record ClientPetResponseDto(Guid Id, Guid ClientId, Guid PetId, bool IsPrimaryOwner, DateTime CreatedAt, DateTime? UpdatedAt);
