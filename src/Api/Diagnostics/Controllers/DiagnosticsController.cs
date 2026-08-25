using Api.Diagnostics.Dtos;
using Api.Diagnostics.Mappings;
using Application.Diagnostics.UseCases;
using Microsoft.AspNetCore.Mvc;

namespace Api.Diagnostics.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DiagnosticsController : ControllerBase
{
    private readonly GetAllDiagnosticsUseCase _getAllUseCase;
    private readonly GetDiagnosticByIdUseCase _getByIdUseCase;
    private readonly CreateDiagnosticUseCase _createUseCase;
    private readonly UpdateDiagnosticUseCase _updateUseCase;
    private readonly DeleteDiagnosticUseCase _deleteUseCase;

    public DiagnosticsController(
        GetAllDiagnosticsUseCase getAllUseCase,
        GetDiagnosticByIdUseCase getByIdUseCase,
        CreateDiagnosticUseCase createUseCase,
        UpdateDiagnosticUseCase updateUseCase,
        DeleteDiagnosticUseCase deleteUseCase)
    {
        _getAllUseCase = getAllUseCase;
        _getByIdUseCase = getByIdUseCase;
        _createUseCase = createUseCase;
        _updateUseCase = updateUseCase;
        _deleteUseCase = deleteUseCase;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<DiagnosticDto>))]
    public async Task<IActionResult> GetAll([FromQuery] bool onlyActive = true, CancellationToken cancellationToken = default)
    {
        var diagnostics = await _getAllUseCase.ExecuteAsync(onlyActive, cancellationToken);
        return Ok(DiagnosticMapping.ToDtoList(diagnostics));
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(DiagnosticDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById([FromRoute] Guid id, CancellationToken cancellationToken = default)
    {
        var diagnostic = await _getByIdUseCase.ExecuteAsync(id, cancellationToken);
        if (diagnostic == null)
            return NotFound(new { Message = $"Diagnóstico con ID {id} no fue encontrado." });

        return Ok(DiagnosticMapping.ToDto(diagnostic));
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(DiagnosticDto))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateDiagnosticDto dto, CancellationToken cancellationToken = default)
    {
        try
        {
            var diagnostic = await _createUseCase.ExecuteAsync(dto.Code, dto.Name, dto.Description, cancellationToken);
            var result = DiagnosticMapping.ToDto(diagnostic);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update([FromRoute] Guid id, [FromBody] UpdateDiagnosticDto dto, CancellationToken cancellationToken = default)
    {
        try
        {
            var diagnostic = await _updateUseCase.ExecuteAsync(id, dto.Code, dto.Name, dto.Description, dto.IsActive, cancellationToken);
            if (diagnostic == null)
                return NotFound(new { Message = $"Diagnóstico con ID {id} no fue encontrado." });

            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete([FromRoute] Guid id, CancellationToken cancellationToken = default)
    {
        var success = await _deleteUseCase.ExecuteAsync(id, cancellationToken);
        if (!success)
            return NotFound(new { Message = $"Diagnóstico con ID {id} no fue encontrado." });

        return NoContent();
    }
}
