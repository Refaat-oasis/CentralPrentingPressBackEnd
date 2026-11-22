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

    [HttpGet("tasweya/{id:int}/document-bundle")]
    public Task<IActionResult> TasweyaWithMoqaysatBundle(int id, CancellationToken ct = default) =>
        Run(() => documents.RenderTasweyaWithMoqaysatBundleAsync(id, ct));

    public sealed record DocumentBundleRequest(
        List<int>? TasweyaIds,
        List<int>? MoqaysaIds,
        string? FileName);

    [HttpPost("documents/bundle")]
    public Task<IActionResult> Bundle([FromBody] DocumentBundleRequest request, CancellationToken ct = default)
    {
        var tasweyaIds = request?.TasweyaIds?.Where(id => id > 0).Distinct().ToList() ?? [];
        var moqaysaIds = request?.MoqaysaIds?.Where(id => id > 0).Distinct().ToList() ?? [];

        if (tasweyaIds.Count == 0 && moqaysaIds.Count == 0)
            return Task.FromResult<IActionResult>(BadRequest(new { message = "يجب تحديد مستند واحد على الأقل للدمج." }));

        if (tasweyaIds.Count + moqaysaIds.Count > 50)
            return Task.FromResult<IActionResult>(BadRequest(new { message = "الحد الأقصى للدمج في الطلب الواحد هو 50 مستنداً." }));

        return Run(() => documents.RenderBundleAsync(tasweyaIds, moqaysaIds, request?.FileName, ct));
    }

    [HttpPost("documents/preview/moqaysa")]
    public Task<IActionResult> PreviewMoqaysaDraft(
        [FromBody] MoqaysaDraftPreviewRequest request,
        [FromQuery] string format = "pdf",
        CancellationToken ct = default)
    {
        if (request == null)
            return Task.FromResult<IActionResult>(BadRequest(new { message = "بيانات معاينة المقايسة مطلوبة." }));

        return Run(() => documents.RenderMoqaysaDraftRequestAsync(request, Parse(format), ct));
    }

    [HttpPost("documents/preview/tasweya")]
    public Task<IActionResult> PreviewTasweyaDraft(
        [FromBody] TasweyaDraftPreviewRequest request,
        [FromQuery] string format = "pdf",
        CancellationToken ct = default)
    {
        if (request == null)
            return Task.FromResult<IActionResult>(BadRequest(new { message = "بيانات معاينة كشف التسوية مطلوبة." }));

        return Run(() => documents.RenderTasweyaDraftAsync(request.ToPrintData(), Parse(format), ct));
    }

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
        var cleanFileName = DocumentService.SanitizeFileName($"PaymentOrder_{id}.{ext}");
        Response.Headers.ContentDisposition = $"{disposition}; filename=\"{cleanFileName}\"";
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
            if (!string.IsNullOrWhiteSpace(doc.TemplateName))
            {
                Response.Headers["X-Document-Template"] = doc.TemplateName;
            }
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
