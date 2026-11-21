using System.Globalization;
using System.Text.RegularExpressions;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace CentralPress.API.Documents;

/// <summary>
/// Fills a .docx template. Tokens: {{Field}}, {{Field.G}} (pounds), {{Field.Q}} (piasters), {{List.3.Field}}.
/// Row markers: {{#List}} repeats the row per item, {{?Field}} keeps the row only when Field is non-zero.
/// Lists in the model must be IEnumerable/IList of object? holding IReadOnlyDictionary&lt;string, object?&gt;.
/// </summary>
public sealed class DocxTemplateFiller
{
    private static readonly Regex Token = new(@"\{\{([^{}]+)\}\}", RegexOptions.Compiled);
    private static readonly Regex Repeat = new(@"\{\{#(\w+)\}\}", RegexOptions.Compiled);
    private static readonly Regex Option = new(@"\{\{\?(\w+)\}\}", RegexOptions.Compiled);
    private const int LineHeight = 400;
    private const int LineBudget = 6800; // tasweya data rows shrink to keep one page

    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, byte[]> PathTemplateCache = new();
    public static void ClearTemplateCache() => PathTemplateCache.Clear();

    private static byte[] ReadTemplateBytes(string path)
    {
        using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var ms = new MemoryStream();
        fs.CopyTo(ms);
        return ms.ToArray();
    }

    public byte[] Fill(string templatePath, IReadOnlyDictionary<string, object?> model)
    {
        byte[] templateBytes = PathTemplateCache.GetOrAdd(templatePath, ReadTemplateBytes);
        using var ms = new MemoryStream(templateBytes.Length);
        ms.Write(templateBytes, 0, templateBytes.Length);
        ms.Position = 0;

        using (var doc = WordprocessingDocument.Open(ms, true))
        {
            var body = doc.MainDocumentPart!.Document.Body!;
            foreach (var row in body.Descendants<TableRow>().ToList())
            {
                var text = row.InnerText;
                var rep = Repeat.Match(text);
                if (rep.Success)
                {
                    if (model.TryGetValue(rep.Groups[1].Value, out var l) && l is IEnumerable<object?> items)
                    {
                        foreach (var it in items.OfType<IReadOnlyDictionary<string, object?>>())
                        {
                            var clone = (TableRow)row.CloneNode(true);
                            Replace(clone, Merge(model, it), rep.Value);
                            row.InsertBeforeSelf(clone);
                        }
                    }
                    row.Remove();
                    continue;
                }

                var opt = Option.Match(text);
                if (opt.Success)
                {
                    if (IsZero(model, opt.Groups[1].Value))
                    {
                        row.Remove();
                    }
                    else
                    {
                        Replace(row, model, opt.Value);
                    }
                }
            }

            ExpandIndexedRows(body, model);

            Replace(body, model, null);

            var lines = body.Descendants<TableRow>().Where(r =>
                r.TableRowProperties?.GetFirstChild<TableRowHeight>() is { } h &&
                h.Val != null && (uint)h.Val == LineHeight && h.HeightType?.Value == HeightRuleValues.Exact).ToList();
            if (lines.Count > 0)
            {
                var h = (uint)Math.Min(LineHeight, LineBudget / lines.Count);
                foreach (var r in lines)
                {
                    r.TableRowProperties!.GetFirstChild<TableRowHeight>()!.Val = new UInt32Value(h);
                }
            }

            // Ensure cantSplit on all table rows across document so Word never splits a row across pages
            foreach (var r in body.Descendants<TableRow>())
            {
                var rPr = r.TableRowProperties ?? r.AppendChild(new TableRowProperties());
                if (rPr.GetFirstChild<CantSplit>() == null)
                {
                    rPr.AppendChild(new CantSplit());
                }
            }

            // Moqaysa main table dynamic row budgeting to guarantee single-page fit
            var moqaysaTable = body.Elements<Table>().FirstOrDefault(t => t.Descendants<TableCell>().Any(c => c.InnerText.Contains("عناصر المقايسة")));
            if (moqaysaTable != null)
            {
                var allRows = moqaysaTable.Elements<TableRow>().ToList();
                // Data rows start after the 5 top header rows (Title/Logos, Requester/Quantity, 3 Column header rows)
                // and end before the 4 bottom rows (Gross, VAT, Grand Total, Signatures)
                var dataRows = allRows.Where((r, idx) => idx >= 5 && idx < allRows.Count - 4).ToList();

                const int DataRowsBudget = 12500; // twips budget for data rows on B4 page
                var targetHeight = (uint)Math.Max(220, Math.Min(320, DataRowsBudget / Math.Max(1, dataRows.Count)));

                // Adjust section margins to standard B4 official template margins
                var sectPr = body.Elements<SectionProperties>().FirstOrDefault();
                if (sectPr != null)
                {
                    var pgMar = sectPr.GetFirstChild<PageMargin>() ?? sectPr.AppendChild(new PageMargin());
                    pgMar.Top = 400;
                    pgMar.Bottom = 260;
                }

                foreach (var r in dataRows)
                {
                    var rPr = r.TableRowProperties ?? r.AppendChild(new TableRowProperties());
                    var trHeight = rPr.GetFirstChild<TableRowHeight>() ?? rPr.AppendChild(new TableRowHeight());
                    trHeight.Val = new UInt32Value(targetHeight);
                    trHeight.HeightType = HeightRuleValues.Exact;

                    // Ensure cell paragraphs have compact spacing in data rows only
                    foreach (var p in r.Descendants<Paragraph>())
                    {
                        var pPr = p.ParagraphProperties ?? p.AppendChild(new ParagraphProperties());
                        var spacing = pPr.SpacingBetweenLines ?? pPr.AppendChild(new SpacingBetweenLines());
                        spacing.Before = "0";
                        spacing.After = "0";
                        spacing.Line = "220";
                        spacing.LineRule = LineSpacingRuleValues.Exact;
                    }
                }
            }

            // Shrink any trailing empty paragraphs so Word/LibreOffice never overflows an extra blank page
            foreach (var p in body.Elements<Paragraph>().ToList())
            {
                if (string.IsNullOrWhiteSpace(p.InnerText) && !p.Descendants<Drawing>().Any())
                {
                    var pPr = p.ParagraphProperties ?? p.AppendChild(new ParagraphProperties());
                    var spacing = pPr.SpacingBetweenLines ?? pPr.AppendChild(new SpacingBetweenLines());
                    spacing.Before = "0";
                    spacing.After = "0";
                    spacing.Line = "20"; // 1pt (20 twips)
                    spacing.LineRule = LineSpacingRuleValues.Exact;

                    var rPr = pPr.ParagraphMarkRunProperties ?? pPr.AppendChild(new ParagraphMarkRunProperties());
                    rPr.AppendChild(new FontSize { Val = "2" }); // 1pt
                }
            }

            doc.MainDocumentPart.Document.Save();
        }

        return ms.ToArray();
    }

    private static void ExpandIndexedRows(Body body, IReadOnlyDictionary<string, object?> model)
    {
        int labourCount = model.TryGetValue("Labour", out var lObj) && lObj is IList<object?> lList ? lList.Count : 0;
        int machineryCount = model.TryGetValue("Machinery", out var mObj) && mObj is IList<object?> mList ? mList.Count : 0;
        int matCount = model.TryGetValue("Mat", out var matObj) && matObj is IList<object?> matList ? matList.Count : 0;

        // 1. Expand Labour rows if count > 11
        if (labourCount > 11)
        {
            var row11 = body.Descendants<TableRow>().FirstOrDefault(r => r.InnerText.Contains("{{Labour.11."));
            if (row11 != null && row11.Parent != null)
            {
                var previousRow = row11;
                for (int i = 12; i <= labourCount; i++)
                {
                    var clone = (TableRow)row11.CloneNode(true);
                    foreach (var t in clone.Descendants<Text>())
                    {
                        if (t.Text != null)
                        {
                            if (t.Text.Contains("{{Labour.11."))
                            {
                                t.Text = t.Text.Replace("{{Labour.11.", $"{{{{Labour.{i}.");
                            }
                            else if (t.Text.Contains("{{Mat."))
                            {
                                // Clear material tokens on cloned labour rows so Mat.12 is not duplicated
                                t.Text = "";
                            }
                        }
                    }
                    row11.Parent.InsertAfter(clone, previousRow);
                    previousRow = clone;
                }
            }
        }

        // 2. Expand Machinery and Materials rows (starting at Machinery.11 / Mat.25)
        int extraMac = Math.Max(0, machineryCount - 11);
        int extraMat = Math.Max(0, matCount - 25);
        int maxExtra = Math.Max(extraMac, extraMat);

        if (maxExtra > 0)
        {
            var row11 = body.Descendants<TableRow>().FirstOrDefault(r => r.InnerText.Contains("{{Machinery.11."));
            if (row11 != null && row11.Parent != null)
            {
                var previousRow = row11;
                for (int i = 0; i < maxExtra; i++)
                {
                    var clone = (TableRow)row11.CloneNode(true);
                    int currentMacIndex = 12 + i;
                    int currentMatIndex = 26 + i;

                    foreach (var t in clone.Descendants<Text>())
                    {
                        if (t.Text != null)
                        {
                            if (t.Text.Contains("{{Machinery.11."))
                            {
                                if (i < extraMac)
                                {
                                    t.Text = t.Text.Replace("{{Machinery.11.", $"{{{{Machinery.{currentMacIndex}.");
                                }
                                else
                                {
                                    // No more machinery, clear machinery cells
                                    t.Text = "";
                                }
                            }
                            else if (t.Text.Contains("{{Mat.25."))
                            {
                                if (i < extraMat)
                                {
                                    t.Text = t.Text.Replace("{{Mat.25.", $"{{{{Mat.{currentMatIndex}.");
                                }
                                else
                                {
                                    // No more materials, clear material cells
                                    t.Text = "";
                                }
                            }
                        }
                    }
                    row11.Parent.InsertAfter(clone, previousRow);
                    previousRow = clone;
                }
            }
        }
    }

    private static Dictionary<string, object?> Merge(IReadOnlyDictionary<string, object?> a, IReadOnlyDictionary<string, object?> b)
    {
        var d = new Dictionary<string, object?>(a);
        foreach (var kv in b) d[kv.Key] = kv.Value;
        return d;
    }

    private static bool IsZero(IReadOnlyDictionary<string, object?> m, string key) =>
        !m.TryGetValue(key, out var v) || v is null || (v is string s && s.Length == 0) ||
        (decimal.TryParse(Convert.ToString(v, CultureInfo.InvariantCulture), NumberStyles.Any,
                          CultureInfo.InvariantCulture, out var d) && d == 0m);

    private static void Replace(OpenXmlElement scope, IReadOnlyDictionary<string, object?> ctx, string? strip)
    {
        foreach (var t in scope.Descendants<Text>())
        {
            var s = t.Text ?? "";
            if (strip != null) s = s.Replace(strip, "");
            t.Text = Token.Replace(s, m => Resolve(m.Groups[1].Value, ctx));
            t.Space = SpaceProcessingModeValues.Preserve;
        }
    }

    private static string SanitizeXml(string? input)
    {
        if (string.IsNullOrEmpty(input)) return string.Empty;
        return new string(input.Where(ch =>
            ch == 0x9 || ch == 0xA || ch == 0xD ||
            (ch >= 0x20 && ch <= 0xD7FF) ||
            (ch >= 0xE000 && ch <= 0xFFFD)).ToArray());
    }

    private static string Resolve(string expr, IReadOnlyDictionary<string, object?> ctx)
    {
        var parts = expr.Trim().Split('.').ToList();
        string? suffix = parts[^1] is "G" or "Q" ? parts[^1] : null;
        if (suffix != null) parts.RemoveAt(parts.Count - 1);

        object? cur = ctx;
        foreach (var p in parts)
        {
            cur = cur switch
            {
                IReadOnlyDictionary<string, object?> d => d.TryGetValue(p, out var v) ? v : null,
                IList<object?> l when int.TryParse(p, out var i) && i >= 1 && i <= l.Count => l[i - 1],
                _ => null
            };
            if (cur is null) return "";
        }

        if (cur is string str) return str.Length == 0 ? "" : SanitizeXml(str);
        if (suffix == null) return SanitizeXml(Convert.ToString(cur, CultureInfo.InvariantCulture) ?? "");

        var value = Math.Round(Convert.ToDecimal(cur, CultureInfo.InvariantCulture), 2, MidpointRounding.AwayFromZero);
        var g = decimal.Truncate(value);
        var q = (int)Math.Abs((value - g) * 100m);
        return suffix == "G" ? g.ToString("#,##0", CultureInfo.InvariantCulture) : q.ToString("00", CultureInfo.InvariantCulture);
    }
}
