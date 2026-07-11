using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using NotesApi.Auth;
using NotesApi.Data;
using NotesApi.Models;

namespace NotesApi.Controllers;

[ApiController]
[Route("api/notes")]
public class NotesController : ControllerBase
{
    private readonly INoteRepository _repository;

    public NotesController(INoteRepository repository)
    {
        _repository = repository;
    }

    /// <summary>key_id del cliente autenticado; toda operación queda acotada a sus notas.</summary>
    private string OwnerKeyId =>
        User.FindFirstValue(ApiKeyAuthenticationHandler.KeyIdClaim)
        ?? throw new InvalidOperationException("No hay key_id en la identidad autenticada.");

    /// <summary>
    /// Lista las notas del cliente (id, título y fechas; el contenido se obtiene por id).
    /// Paginación por keyset: enviar afterId = nextAfterId de la página anterior.
    /// Con search se filtra por texto completo sobre título y contenido.
    /// </summary>
    [HttpGet]
    [ProducesResponseType<PagedResult<NoteSummary>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<NoteSummary>>> GetAll(
        CancellationToken cancellationToken,
        [FromQuery][Range(0, int.MaxValue)] int afterId = 0,
        [FromQuery][Range(1, 100)] int pageSize = 20,
        [FromQuery][MaxLength(100)] string? search = null)
    {
        return Ok(await _repository.GetPageAsync(OwnerKeyId, afterId, pageSize, search, cancellationToken));
    }

    /// <summary>Obtiene una nota por su id.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType<Note>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Note>> GetById(int id, CancellationToken cancellationToken)
    {
        var note = await _repository.GetByIdAsync(OwnerKeyId, id, cancellationToken);
        return note is null ? NotFound() : Ok(note);
    }

    /// <summary>Crea una nota nueva.</summary>
    [HttpPost]
    [ProducesResponseType<Note>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Note>> Create(NoteRequest request, CancellationToken cancellationToken)
    {
        var created = await _repository.CreateAsync(
            OwnerKeyId, new Note { Title = request.Title, Text = request.Text }, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    /// <summary>Actualiza una nota existente.</summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, NoteRequest request, CancellationToken cancellationToken)
    {
        var updated = await _repository.UpdateAsync(
            OwnerKeyId, new Note { Id = id, Title = request.Title, Text = request.Text }, cancellationToken);
        return updated ? NoContent() : NotFound();
    }

    /// <summary>Elimina una nota.</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        return await _repository.DeleteAsync(OwnerKeyId, id, cancellationToken) ? NoContent() : NotFound();
    }
}
