using Microsoft.AspNetCore.Hosting;
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
    IWebHostEnvironment env)
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
        var docx = filler.Fill(TemplatePath("Moqaysa.docx"), MoqaysaTokens.Build(d));
        return await Pack(docx, $"moqaysa-{d.JobOrderNo}", format, ct);
    }

    public async Task<RenderedDocument> RenderTasweyaAsync(int id, DocFormat format, CancellationToken ct)
    {
        var d = await data.LoadTasweyaAsync(id, ct);
        var (template, model) = TasweyaTokens.Build(d, words);
        var docx = filler.Fill(TemplatePath(template), model);
        return await Pack(docx, $"tasweya-{d.SettlementNo}", format, ct);
    }

    private async Task<RenderedDocument> Pack(byte[] docx, string name, DocFormat format, CancellationToken ct)
    {
        return format == DocFormat.Docx
            ? new RenderedDocument(docx, DocxType, name + ".docx")
            : new RenderedDocument(await pdf.ToPdfAsync(docx, ct), PdfType, name + ".pdf");
    }
}
