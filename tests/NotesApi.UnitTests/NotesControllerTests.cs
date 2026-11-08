using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NotesApi.Controllers;
using NotesApi.Data;
using NotesApi.Models;

namespace NotesApi.UnitTests;

public class NotesControllerTests
{
    // T013
    [Fact]
    public async Task IdentidadSinClaimApiKeyId_LanzaInvalidOperationException()
    {
        var controller = new NotesController(new UnusedNoteRepository())
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(
                        [new Claim(ClaimTypes.Name, "cliente")], "Test")),
                },
            },
        };

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => controller.GetById(1, CancellationToken.None));
        Assert.Equal("No hay key_id en la identidad autenticada.", exception.Message);
    }

    /// <summary>El acceso a OwnerKeyId debe lanzar antes de llegar al repositorio.</summary>
    private sealed class UnusedNoteRepository : INoteRepository
    {
        public Task<PagedResult<NoteSummary>> GetPageAsync(
            string ownerKeyId, int afterId, int pageSize, string? search, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task<Note?> GetByIdAsync(string ownerKeyId, int id, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task<Note> CreateAsync(
            string ownerKeyId, NoteRequest request, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task<bool> UpdateAsync(
            string ownerKeyId, int id, NoteRequest request, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task<bool> DeleteAsync(string ownerKeyId, int id, CancellationToken cancellationToken)
            => throw new NotSupportedException();
    }
}
