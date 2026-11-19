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
    private const int LineHeight = 600;
    private const int LineBudget = 10400; // tasweya data rows shrink to keep one page

    public byte[] Fill(string templatePath, IReadOnlyDictionary<string, object?> model)
    {
        using var ms = new MemoryStream();
        using (var fs = File.OpenRead(templatePath))
        {
            fs.CopyTo(ms);
        }

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

            // Shrink any trailing empty paragraphs so Word/LibreOffice never overflows an extra blank page
            foreach (var p in body.Elements<Paragraph>().ToList())
            {
                if (string.IsNullOrWhiteSpace(p.InnerText))
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

        if (cur is string str && str.Length == 0) return "";
        if (suffix == null) return Convert.ToString(cur, CultureInfo.InvariantCulture) ?? "";

        var value = Math.Round(Convert.ToDecimal(cur, CultureInfo.InvariantCulture), 2, MidpointRounding.AwayFromZero);
        var g = decimal.Truncate(value);
        var q = (int)Math.Abs((value - g) * 100m);
        return suffix == "G" ? g.ToString("#,##0", CultureInfo.InvariantCulture) : q.ToString("00", CultureInfo.InvariantCulture);
    }
}
