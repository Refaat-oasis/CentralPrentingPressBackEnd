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

    [HttpGet("payment-order/{id:int}/document")]
    public async Task<IActionResult> PaymentOrder(
        int id,
        [FromServices] CentralPress.API.Services.IPaymentOrderService paymentOrderService,
        [FromQuery] string format = "pdf",
        CancellationToken ct = default)
    {
        var docFormat = Parse(format);
        byte[]? bytes = docFormat == DocFormat.Docx
            ? await paymentOrderService.ExportWordAsync(id, ct)
            : await paymentOrderService.ExportPdfAsync(id, ct);

        if (bytes == null) return NotFound(new { message = "أمر الدفع غير موجود" });

        var ext = docFormat == DocFormat.Docx ? "docx" : "pdf";
        var mime = docFormat == DocFormat.Docx
            ? "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
            : "application/pdf";
        var disposition = docFormat == DocFormat.Pdf ? "inline" : "attachment";
        Response.Headers.ContentDisposition = $"{disposition}; filename=\"PaymentOrder_{id}.{ext}\"";
        return new FileContentResult(bytes, mime);
    }

    private static DocFormat Parse(string f) =>
        string.Equals(f, "docx", StringComparison.OrdinalIgnoreCase) ? DocFormat.Docx : DocFormat.Pdf;

    private async Task<IActionResult> Run(Func<Task<RenderedDocument>> render)
    {
        try
        {
            var doc = await render();
            var disposition = doc.ContentType == "application/pdf" ? "inline" : "attachment";
            var cleanFileName = DocumentService.SanitizeFileName(doc.FileName);
            Response.Headers.ContentDisposition = $"{disposition}; filename=\"{cleanFileName}\"";
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
