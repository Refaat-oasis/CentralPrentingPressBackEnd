using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace CentralPress.API.Documents;

/// <summary>
/// The unified document rendering service. Preview, print, PDF, and Word all generate from this single pipeline.
/// </summary>
public sealed class DocumentService(
    IPrintDataSource data,
    DocxTemplateFiller filler,
    PdfConverter pdf,
    IAmountInWords words,
    IOptions<DocumentOptions> options,
    IWebHostEnvironment env,
    IMemoryCache cache)
{
    private const string DocxType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
    private const string PdfType = "application/pdf";

    private string TemplatePath(string file)
    {
        var primary = Path.Combine(env.ContentRootPath, options.Value.TemplatesFolder, file);
        if (File.Exists(primary)) return primary;

        var appBase = AppContext.BaseDirectory;
        var alt1 = Path.Combine(appBase, options.Value.TemplatesFolder, file);
        if (File.Exists(alt1)) return alt1;

        var alt2 = Path.GetFullPath(Path.Combine(appBase, "..", "..", "..", "..", "CentralPress.API", options.Value.TemplatesFolder, file));
        if (File.Exists(alt2)) return alt2;

        return primary;
    }

    public async Task<RenderedDocument> RenderMoqaysaAsync(int id, DocFormat format, CancellationToken ct)
    {
        var d = await data.LoadMoqaysaAsync(id, ct);
        return await RenderMoqaysaDraftAsync(d, format, ct);
    }

    public async Task<RenderedDocument> RenderMoqaysaDraftAsync(MoqaysaPrintData d, DocFormat format, CancellationToken ct)
    {
        const string template = "moqaysa.docx";
        var docx = filler.Fill(TemplatePath(template), MoqaysaTokens.Build(d));
        return await Pack(docx, $"moqaysa-{d.JobOrderNo}", format, ct, template);
    }

    public async Task<RenderedDocument> RenderMoqaysaDraftRequestAsync(MoqaysaDraftPreviewRequest request, DocFormat format, CancellationToken ct)
    {
        var catalogNames = await data.LoadActiveMachineryNamesAsync(ct);
        return await RenderMoqaysaDraftAsync(request.ToPrintData(catalogNames), format, ct);
    }

    public async Task<RenderedDocument> RenderTasweyaAsync(int id, DocFormat format, CancellationToken ct)
    {
        var d = await data.LoadTasweyaAsync(id, ct);
        return await RenderTasweyaDraftAsync(d, format, ct);
    }

    public async Task<RenderedDocument> RenderTasweyaDraftAsync(TasweyaPrintData d, DocFormat format, CancellationToken ct)
    {
        var (template, model) = TasweyaTokens.Build(d, words);
        var docx = filler.Fill(TemplatePath(template), model);
        return await Pack(docx, $"tasweya-{d.SettlementNo}", format, ct, template);
    }

    public async Task<RenderedDocument> RenderTasweyaWithMoqaysatBundleAsync(int tasweyaId, CancellationToken ct)
    {
        var linkedMoqaysaIds = await data.LoadLinkedMoqaysaIdsAsync(tasweyaId, ct);
        var tasweyaDoc = await RenderTasweyaAsync(tasweyaId, DocFormat.Pdf, ct);

        var pdfParts = new List<byte[]> { tasweyaDoc.Bytes };
        foreach (var moqaysaId in linkedMoqaysaIds)
        {
            var moqaysaDoc = await RenderMoqaysaAsync(moqaysaId, DocFormat.Pdf, ct);
            pdfParts.Add(moqaysaDoc.Bytes);
        }

        var merged = PdfMerger.Merge(pdfParts);
        var baseName = Path.GetFileNameWithoutExtension(tasweyaDoc.FileName);
        var cleanName = SanitizeFileName($"{baseName}_with_moqaysat");
        var templateLabel = $"{tasweyaDoc.TemplateName ?? "Tasweya"}+moqaysa.docx";
        return new RenderedDocument(merged, PdfType, cleanName + ".pdf", templateLabel);
    }

    public async Task<RenderedDocument> RenderBundleAsync(
        IReadOnlyList<int> tasweyaIds,
        IReadOnlyList<int> moqaysaIds,
        string? bundleName,
        CancellationToken ct)
    {
        var pdfParts = new List<byte[]>();
        var usedTemplates = new List<string>();

        foreach (var tasweyaId in tasweyaIds)
        {
            var doc = await RenderTasweyaAsync(tasweyaId, DocFormat.Pdf, ct);
            pdfParts.Add(doc.Bytes);
            if (!string.IsNullOrWhiteSpace(doc.TemplateName) && !usedTemplates.Contains(doc.TemplateName))
                usedTemplates.Add(doc.TemplateName);
        }

        foreach (var moqaysaId in moqaysaIds)
        {
            var doc = await RenderMoqaysaAsync(moqaysaId, DocFormat.Pdf, ct);
            pdfParts.Add(doc.Bytes);
            if (!string.IsNullOrWhiteSpace(doc.TemplateName) && !usedTemplates.Contains(doc.TemplateName))
                usedTemplates.Add(doc.TemplateName);
        }

        if (pdfParts.Count == 0)
            throw new PrintDataException("يجب تحديد مستند واحد على الأقل للتصدير المجمع.");

        var merged = PdfMerger.Merge(pdfParts);
        var cleanName = SanitizeFileName(string.IsNullOrWhiteSpace(bundleName) ? "Documents_Bundle" : Path.GetFileNameWithoutExtension(bundleName));
        var templateLabel = usedTemplates.Count > 0 ? string.Join("+", usedTemplates) : "bundle";
        return new RenderedDocument(merged, PdfType, cleanName + ".pdf", templateLabel);
    }

    public static string SanitizeFileName(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return "document";
        var normalized = Helpers.ArabicDigits.NormalizeToAscii(name);
        var invalid = Path.GetInvalidFileNameChars();
        var clean = new string(normalized.Select(c => invalid.Contains(c) || c == '/' || c == '\\' ? '_' : c).ToArray()).Trim();
        return string.IsNullOrWhiteSpace(clean) ? "document" : clean;
    }

    private async Task<RenderedDocument> Pack(byte[] docx, string name, DocFormat format, CancellationToken ct, string? templateName = null)
    {
        string cleanName = SanitizeFileName(name);

        if (format == DocFormat.Docx)
        {
            return new RenderedDocument(docx, DocxType, cleanName + ".docx", templateName);
        }

        // Cache PDF by content hash of docx so repeated views/downloads are instant (< 5ms)
        string cacheKey = $"doc_pdf_{cleanName}_{Convert.ToBase64String(System.Security.Cryptography.SHA256.HashData(docx))}";
        if (cache.TryGetValue(cacheKey, out byte[]? cachedPdf) && cachedPdf != null)
        {
            return new RenderedDocument(cachedPdf, PdfType, cleanName + ".pdf", templateName);
        }

        var pdfBytes = await pdf.ToPdfAsync(docx, ct);
        cache.Set(cacheKey, pdfBytes, new MemoryCacheEntryOptions
        {
            SlidingExpiration = TimeSpan.FromMinutes(30),
            AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(4)
        });

        return new RenderedDocument(pdfBytes, PdfType, cleanName + ".pdf", templateName);
    }
}
