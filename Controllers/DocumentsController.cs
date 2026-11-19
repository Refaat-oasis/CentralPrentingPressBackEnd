using CentralPress.API.Documents;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CentralPress.API.Controllers;

[ApiController]
[Route("api")]
[Authorize]
public sealed class DocumentsController(DocumentService documents) : ControllerBase
{
    [HttpGet("moqaysa/{id:int}/document")]
    public Task<IActionResult> Moqaysa(int id, [FromQuery] string format = "pdf", CancellationToken ct = default) =>
        Run(() => documents.RenderMoqaysaAsync(id, Parse(format), ct));

    [HttpGet("tasweya/{id:int}/document")]
    public Task<IActionResult> Tasweya(int id, [FromQuery] string format = "pdf", CancellationToken ct = default) =>
        Run(() => documents.RenderTasweyaAsync(id, Parse(format), ct));

    private static DocFormat Parse(string f) =>
        string.Equals(f, "docx", StringComparison.OrdinalIgnoreCase) ? DocFormat.Docx : DocFormat.Pdf;

    private async Task<IActionResult> Run(Func<Task<RenderedDocument>> render)
    {
        try
        {
            var doc = await render();
            var disposition = doc.ContentType == "application/pdf" ? "inline" : "attachment";
            Response.Headers.ContentDisposition = $"{disposition}; filename=\"{doc.FileName}\"";
            return new FileContentResult(doc.Bytes, doc.ContentType);
        }
        catch (PrintDataException ex)
        {
            return UnprocessableEntity(new { message = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }
}
