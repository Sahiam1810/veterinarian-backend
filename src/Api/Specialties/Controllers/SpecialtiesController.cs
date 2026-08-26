using Api.Specialties.Dtos;
using Api.Specialties.Mappings;
using Application.Specialties.UseCases;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Api.Specialties.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class SpecialtiesController(ISender sender) : ControllerBase
{
    [HttpGet]
    [EndpointSummary("Obtiene las especialidades")]
    [EndpointDescription("Lista todas las especialidades registradas.")]
    public async Task<ActionResult<IReadOnlyCollection<SpecialtyResponseDto>>> GetAll(CancellationToken ct) => Ok((await sender.Send(new GetAllSpecialtiesQuery(), ct)).Select(x => x.ToDto()).ToArray());

    [HttpGet("{id:guid}")]
    [EndpointSummary("Obtiene una especialidad")]
    [EndpointDescription("Busca una especialidad por su identificador.")]
    public async Task<ActionResult<SpecialtyResponseDto>> GetById(Guid id, CancellationToken ct) => Ok((await sender.Send(new GetSpecialtyByIdQuery(id), ct)).ToDto());

    [HttpPost]
    [EndpointSummary("Crea una especialidad")]
    [EndpointDescription("Registra una nueva especialidad.")]
    public async Task<ActionResult<SpecialtyResponseDto>> Create(CreateSpecialtyDto dto, CancellationToken ct)
    {
        var id = await sender.Send(new CreateSpecialtyCommand(dto.Name, dto.Description), ct);
        var specialty = await sender.Send(new GetSpecialtyByIdQuery(id), ct);
        return CreatedAtAction(nameof(GetById), new { id }, specialty.ToDto());
    }

    [HttpPut("{id:guid}")]
    [EndpointSummary("Actualiza una especialidad")]
    [EndpointDescription("Actualiza los datos de una especialidad existente.")]
    public async Task<IActionResult> Update(Guid id, UpdateSpecialtyDto dto, CancellationToken ct)
    {
        await sender.Send(new UpdateSpecialtyCommand(id, dto.Name, dto.Description), ct);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [EndpointSummary("Elimina una especialidad")]
    [EndpointDescription("Elimina una especialidad por su identificador.")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await sender.Send(new DeleteSpecialtyCommand(id), ct);
        return NoContent();
    }
}
