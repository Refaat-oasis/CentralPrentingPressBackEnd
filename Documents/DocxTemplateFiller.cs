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
                    var trH = r.TableRowProperties!.GetFirstChild<TableRowHeight>()!;
                    trH.Val = new UInt32Value(h);
                    // Use AtLeast so wrapped long Arabic account names / job order lists are never clipped
                    trH.HeightType = HeightRuleValues.AtLeast;
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
                // Body rows span from R05 (أجور عمال) through R46 (القيمة الإجمالية), right above R47 (Signatures)
                var bodyRows = allRows.Where((r, idx) => idx >= 5 && idx < allRows.Count - 1).ToList();

                // Normalize R00 header parentheses symmetry: "(     12000)" -> "(  12000  )"
                // and apply bold weight when template's '(AH) Manal Black' display font falls back to Times New Roman.
                if (allRows.Count > 0)
                {
                    var r0Runs = allRows[0].Descendants<Run>().ToList();
                    for (int ri = 0; ri < r0Runs.Count; ri++)
                    {
                        var rPr0 = r0Runs[ri].RunProperties ?? r0Runs[ri].PrependChild(new RunProperties());
                        if (rPr0.GetFirstChild<Bold>() == null) rPr0.PrependChild(new Bold());
                        if (rPr0.GetFirstChild<BoldComplexScript>() == null) rPr0.AppendChild(new BoldComplexScript());

                        var t = r0Runs[ri].GetFirstChild<Text>();
                        if (t?.Text == null) continue;
                        var trimmed = t.Text.Trim();
                        if (trimmed == "(")
                        {
                            t.Text = "(  ";
                            t.Space = SpaceProcessingModeValues.Preserve;
                            // Collapse stray extra whitespace-only run immediately following '('
                            if (ri + 1 < r0Runs.Count && string.IsNullOrWhiteSpace(r0Runs[ri + 1].InnerText))
                            {
                                r0Runs[ri + 1].Remove();
                                r0Runs.RemoveAt(ri + 1);
                            }
                        }
                        else if (trimmed == ")")
                        {
                            t.Text = "  )";
                            t.Space = SpaceProcessingModeValues.Preserve;
                        }
                    }
                }

                // Ensure R01 'سعر الوحدة' label includes un-underlined ' : ' matching 'الجهة الطالبة : ' and 'الكمية المطلوبة : ',
                // and enlarge the tiny 10pt (sz=20) dotted-underline value runs (Requester, Quantity, UnitPrice, Specs)
                // to 13pt bold (sz=26) so filled header values match the 14pt bold labels cleanly.
                if (allRows.Count > 1)
                {
                    foreach (var t in allRows[1].Descendants<Text>())
                    {
                        if (t.Text != null && t.Text.Trim() == "سعر الوحدة")
                        {
                            t.Text = "سعر الوحدة : ";
                            t.Space = SpaceProcessingModeValues.Preserve;
                            break;
                        }
                    }

                    foreach (var run in allRows[1].Descendants<Run>())
                    {
                        var rPr1 = run.RunProperties;
                        if (rPr1?.Underline?.Val?.Value != UnderlineValues.Dotted) continue;

                        if (rPr1.GetFirstChild<Bold>() == null) rPr1.PrependChild(new Bold());
                        if (rPr1.GetFirstChild<BoldComplexScript>() == null) rPr1.AppendChild(new BoldComplexScript());

                        var sz = rPr1.GetFirstChild<FontSize>() ?? rPr1.AppendChild(new FontSize());
                        sz.Val = "26";
                        var szCs = rPr1.GetFirstChild<FontSizeComplexScript>() ?? rPr1.AppendChild(new FontSizeComplexScript());
                        szCs.Val = "26";
                    }
                }

                // Adjust section margins to standard B4 official template margins
                var sectPr = body.Elements<SectionProperties>().FirstOrDefault();
                if (sectPr != null)
                {
                    var pgMar = sectPr.GetFirstChild<PageMargin>() ?? sectPr.AppendChild(new PageMargin());
                    pgMar.Top = 340;
                    pgMar.Bottom = 220;
                }

                // Convert heavy outer double borders (double:18, which LibreOffice inflates to 6.75pt)
                // into clean single bold outer borders (single:18 = 2.25pt) across the Moqaysa table.
                foreach (var border in moqaysaTable.Descendants<BorderType>())
                {
                    if (border.Val?.Value == BorderValues.Double)
                    {
                        border.Val = BorderValues.Single;
                    }
                }

                // Preserve the original template's exact 2-tier row rhythm (item rows vs uniform summary rows R17 & R30..R46)
                // scaled proportionally to a strict single-page B4 budget so there is zero height jump at R44.
                const double BodyRowsBudget = 15380.0;
                var desiredHeights = bodyRows.Select(r =>
                {
                    var origH = r.TableRowProperties?.GetFirstChild<TableRowHeight>()?.Val?.Value ?? 350u;
                    return origH >= 380u ? 390.0 : 348.0;
                }).ToList();
                double totalDesired = desiredHeights.Sum();
                double scale = totalDesired > BodyRowsBudget ? BodyRowsBudget / totalDesired : 1.0;

                for (int bi = 0; bi < bodyRows.Count; bi++)
                {
                    var r = bodyRows[bi];
                    uint h = (uint)Math.Max(220, (int)Math.Round(desiredHeights[bi] * scale));
                    var rPr = r.TableRowProperties ?? r.PrependChild(new TableRowProperties());
                    var trHeight = rPr.GetFirstChild<TableRowHeight>() ?? rPr.AppendChild(new TableRowHeight());
                    trHeight.Val = new UInt32Value(h);
                    trHeight.HeightType = HeightRuleValues.Exact;
                }

                // Vertically center ALL data & total rows (R05..R46), cap inflated paragraph-mark font sizes (e.g. sz=36),
                // and normalize OpenXML element order so LibreOffice honors vAlign="center" and spacing on EVERY cell.
                for (int idx = 5; idx < allRows.Count - 1; idx++)
                {
                    var r = allRows[idx];
                    var rowCells = r.Elements<TableCell>().ToList();
                    for (int ci = 0; ci < rowCells.Count; ci++)
                    {
                        var cell = rowCells[ci];
                        var tcPr = cell.TableCellProperties ?? cell.PrependChild(new TableCellProperties());
                        var vAlign = tcPr.GetFirstChild<TableCellVerticalAlignment>() ?? tcPr.AppendChild(new TableCellVerticalAlignment());
                        vAlign.Val = TableVerticalAlignmentValues.Center;

                        var tcMar = tcPr.GetFirstChild<TableCellMargin>() ?? tcPr.AppendChild(new TableCellMargin());
                        tcMar.GetFirstChild<TopMargin>()?.Remove();
                        tcMar.AppendChild(new TopMargin { Width = "0", Type = TableWidthUnitValues.Dxa });
                        tcMar.GetFirstChild<BottomMargin>()?.Remove();
                        tcMar.AppendChild(new BottomMargin { Width = "0", Type = TableWidthUnitValues.Dxa });
                        NormalizeTableCellMarginOrder(tcMar);

                        foreach (var p in cell.Elements<Paragraph>())
                        {
                            var pPr = p.ParagraphProperties ?? p.PrependChild(new ParagraphProperties());
                            var spacing = pPr.SpacingBetweenLines ?? pPr.AppendChild(new SpacingBetweenLines());
                            spacing.Before = "0";
                            spacing.After = "0";
                            spacing.Line = "240";
                            spacing.LineRule = LineSpacingRuleValues.Auto;

                            // Cap oversized paragraph-mark font sizes (some cells had sz=36 = 18pt pushing 13pt digits down!)
                            var pmRPr = pPr.ParagraphMarkRunProperties;
                            if (pmRPr != null)
                            {
                                CapFontSize(pmRPr, 24);
                            }

                            // Keep machinery label column (C0) at <= 13pt (26), preserving 11pt (22) on long labels
                            // like 'م. تشغيل الخامات' so Arabic letters stay crisp inside borders and dotted lines.
                            if (ci == 0)
                            {
                                var trimmedLabel = cell.InnerText.Trim();
                                int maxLabelHalfPts = trimmedLabel.Length >= 15 ? 22 : 26;
                                foreach (var runRPr in p.Descendants<RunProperties>())
                                {
                                    CapFontSize(runRPr, maxLabelHalfPts);
                                }

                                // Align 'جملة الخامات' centered to match all other 14 summary total labels in C0
                                if (trimmedLabel == "جملة الخامات")
                                {
                                    var jc = pPr.Justification ?? pPr.AppendChild(new Justification());
                                    jc.Val = JustificationValues.Center;
                                }
                            }
                            else
                            {
                                // Empty cells sometimes carry stray sz=36 runs that shift the row baseline
                                foreach (var run in p.Elements<Run>())
                                {
                                    if (string.IsNullOrWhiteSpace(run.InnerText) && run.RunProperties != null)
                                    {
                                        CapFontSize(run.RunProperties, 22);
                                    }
                                }
                            }

                            NormalizeParagraphPropertiesOrder(pPr);
                        }

                        NormalizeTableCellPropertiesOrder(tcPr);
                    }
                }

                // Fix extra dotted lines & top-aligned numbers inside the 3 left material summary boxes
                // (الجملة : / المرتجع : / صافى الجملة) where label cell C10 has vMerge across 2 rows but C11 & C12 did not.
                for (int i = 5; i < allRows.Count - 2; i++)
                {
                    var topCells = allRows[i].Elements<TableCell>().ToList();
                    var botCells = allRows[i + 1].Elements<TableCell>().ToList();
                    if (topCells.Count != botCells.Count || topCells.Count < 13) continue;

                    for (int c = 0; c < topCells.Count - 2; c++)
                    {
                        var vm = topCells[c].TableCellProperties?.GetFirstChild<VerticalMerge>();
                        if (vm?.Val?.Value != MergedCellValues.Restart) continue;

                        var labelText = topCells[c].InnerText;
                        if (!labelText.Contains("الجملة") && !labelText.Contains("المرتجع")) continue;

                        for (int k = c + 1; k <= c + 2; k++)
                        {
                            var topTcPr = topCells[k].TableCellProperties ?? topCells[k].PrependChild(new TableCellProperties());
                            var topVm = topTcPr.GetFirstChild<VerticalMerge>();
                            if (topVm == null)
                                topTcPr.AppendChild(new VerticalMerge { Val = MergedCellValues.Restart });
                            else
                                topVm.Val = MergedCellValues.Restart;

                            var topVAlign = topTcPr.GetFirstChild<TableCellVerticalAlignment>() ?? topTcPr.AppendChild(new TableCellVerticalAlignment());
                            topVAlign.Val = TableVerticalAlignmentValues.Center;

                            var topBorders = topTcPr.GetFirstChild<TableCellBorders>() ?? topTcPr.AppendChild(new TableCellBorders());
                            topBorders.BottomBorder = new BottomBorder { Val = BorderValues.None };

                            var botTcPr = botCells[k].TableCellProperties ?? botCells[k].PrependChild(new TableCellProperties());
                            var botVm = botTcPr.GetFirstChild<VerticalMerge>();
                            if (botVm == null)
                                botTcPr.AppendChild(new VerticalMerge { Val = MergedCellValues.Continue });
                            else
                                botVm.Val = MergedCellValues.Continue;

                            var botBorders = botTcPr.GetFirstChild<TableCellBorders>() ?? botTcPr.AppendChild(new TableCellBorders());
                            botBorders.TopBorder = new TopBorder { Val = BorderValues.None };

                            NormalizeTableCellPropertiesOrder(topTcPr);
                            NormalizeTableCellPropertiesOrder(botTcPr);
                        }
                    }
                }

                // Add comfortable vertical breathing room above the bottom signatures row (المختص / رئيس وحدة التكاليف / مدير عام إدارة المطبعة)
                var sigRow = allRows.LastOrDefault(r => r.InnerText.Contains("المختص") || r.InnerText.Contains("رئيس وحدة التكاليف"));
                if (sigRow != null)
                {
                    var rPr = sigRow.TableRowProperties ?? sigRow.AppendChild(new TableRowProperties());
                    var trHeight = rPr.GetFirstChild<TableRowHeight>() ?? rPr.AppendChild(new TableRowHeight());
                    trHeight.Val = new UInt32Value(660u);
                    trHeight.HeightType = HeightRuleValues.AtLeast;

                    foreach (var cell in sigRow.Elements<TableCell>())
                    {
                        var tcPr = cell.TableCellProperties ?? cell.PrependChild(new TableCellProperties());
                        var tcMar = tcPr.GetFirstChild<TableCellMargin>() ?? tcPr.AppendChild(new TableCellMargin());
                        var topMar = tcMar.GetFirstChild<TopMargin>();
                        if (topMar != null) topMar.Remove();
                        tcMar.AppendChild(new TopMargin { Width = "200", Type = TableWidthUnitValues.Dxa });
                        NormalizeTableCellMarginOrder(tcMar);
                        NormalizeTableCellPropertiesOrder(tcPr);

                        foreach (var p in cell.Elements<Paragraph>())
                        {
                            var pPr = p.ParagraphProperties ?? p.PrependChild(new ParagraphProperties());
                            var spacing = pPr.SpacingBetweenLines ?? pPr.AppendChild(new SpacingBetweenLines());
                            spacing.Before = "180";
                            spacing.After = "0";
                            spacing.Line = "240";
                            spacing.LineRule = LineSpacingRuleValues.Auto;
                            NormalizeParagraphPropertiesOrder(pPr);
                        }
                    }
                }

                // Prevent LibreOffice from crowding the first right-side Arabic letter on the right label column (C0)
                // adjacent to the outer bold right border (e.g. مصاريف الآلات / الخامات / م. تشغيل الخامات)
                for (int idx = 5; idx < allRows.Count - 1; idx++)
                {
                    var cell = allRows[idx].Elements<TableCell>().FirstOrDefault();
                    var tcPr = cell?.TableCellProperties;
                    if (cell == null || tcPr == null) continue;

                    var tcMar = tcPr.GetFirstChild<TableCellMargin>() ?? tcPr.AppendChild(new TableCellMargin());
                    var startMar = tcMar.GetFirstChild<StartMargin>();
                    if (startMar == null || !int.TryParse(startMar.Width?.Value, out int sw) || sw < 90)
                    {
                        if (startMar != null) startMar.Remove();
                        tcMar.AppendChild(new StartMargin { Width = "90", Type = TableWidthUnitValues.Dxa });
                    }
                    var rightMar = tcMar.GetFirstChild<RightMargin>();
                    if (rightMar == null || !int.TryParse(rightMar.Width?.Value, out int rw) || rw < 90)
                    {
                        if (rightMar != null) rightMar.Remove();
                        tcMar.AppendChild(new RightMargin { Width = "90", Type = TableWidthUnitValues.Dxa });
                    }
                    NormalizeTableCellMarginOrder(tcMar);
                    NormalizeTableCellPropertiesOrder(tcPr);
                    foreach (var p in cell.Elements<Paragraph>())
                    {
                        var pPr = p.ParagraphProperties ?? p.PrependChild(new ParagraphProperties());
                        var jcVal = pPr.Justification?.Val?.Value;
                        if (jcVal == JustificationValues.Start || jcVal == JustificationValues.Right)
                        {
                            var ind = pPr.Indentation ?? pPr.AppendChild(new Indentation());
                            ind.Start = "60";
                            ind.Right = "60";
                            NormalizeParagraphPropertiesOrder(pPr);
                        }
                    }
                }

                // Widen & expand the top-right AU-GEA-CN-FRM-001 textbox slightly so 'رقم وتاريخ الإصدار: 01-01/ 05/ 2015'
                // stays cleanly on 1 line (matching Word) instead of wrapping '2015' onto a 3rd line in LibreOffice.
                foreach (var drawing in body.Descendants<Drawing>())
                {
                    if (!drawing.InnerText.Contains("AU-GEA-CN-FRM-001", StringComparison.Ordinal)) continue;
                    foreach (var posH in drawing.Descendants<DocumentFormat.OpenXml.Drawing.Wordprocessing.HorizontalPosition>())
                    {
                        var offset = posH.GetFirstChild<DocumentFormat.OpenXml.Drawing.Wordprocessing.PositionOffset>();
                        if (offset != null && long.TryParse(offset.Text, out long x) && x > 6780000L)
                            offset.Text = "6780000";
                    }
                    foreach (var extent in drawing.Descendants<DocumentFormat.OpenXml.Drawing.Wordprocessing.Extent>())
                    {
                        if (extent.Cx == null || extent.Cx.Value < 2000000L)
                            extent.Cx = 2000000L;
                        if (extent.Cy == null || extent.Cy.Value < 680000L)
                            extent.Cy = 680000L;
                    }
                    foreach (var ext in drawing.Descendants<DocumentFormat.OpenXml.Drawing.Extents>())
                    {
                        if (ext.Cx == null || ext.Cx.Value < 2000000L)
                            ext.Cx = 2000000L;
                        if (ext.Cy == null || ext.Cy.Value < 680000L)
                            ext.Cy = 680000L;
                    }
                    foreach (var bodyPr in drawing.Descendants<DocumentFormat.OpenXml.Office2010.Word.DrawingShape.TextBodyProperties>())
                    {
                        bodyPr.LeftInset = 10000;
                        bodyPr.RightInset = 10000;
                    }
                }
            }
            else
            {
                // Tasweya polish: vertically center the grand total / tafqeet row and give signatures clean top spacing
                var tasweyaTable = body.Elements<Table>().FirstOrDefault(t =>
                    t.InnerText.Contains("المراقب المالي") || t.InnerText.Contains("رئيس الشئون المالية"));
                if (tasweyaTable != null)
                {
                    var tRows = tasweyaTable.Elements<TableRow>().ToList();
                    var sigIdx = tRows.FindLastIndex(r =>
                        r.InnerText.Contains("المراقب المالي") || r.InnerText.Contains("رئيس الشئون المالية"));

                    if (sigIdx > 0)
                    {
                        // Center the grand total & AmountInWords row right above signatures with clean breathing room on all 4 sides
                        var grandRow = tRows[sigIdx - 1];
                        var grandRPr = grandRow.TableRowProperties ?? grandRow.PrependChild(new TableRowProperties());
                        var grandTrH = grandRPr.GetFirstChild<TableRowHeight>() ?? grandRPr.AppendChild(new TableRowHeight());
                        if (grandTrH.Val == null || grandTrH.Val.Value < 860u)
                            grandTrH.Val = new UInt32Value(860u);
                        grandTrH.HeightType = HeightRuleValues.AtLeast;

                        var grandCells = grandRow.Elements<TableCell>().ToList();
                        for (int gi = 0; gi < grandCells.Count; gi++)
                        {
                            var cell = grandCells[gi];
                            var tcPr = cell.TableCellProperties ?? cell.PrependChild(new TableCellProperties());
                            var vAlign = tcPr.GetFirstChild<TableCellVerticalAlignment>() ?? tcPr.AppendChild(new TableCellVerticalAlignment());
                            vAlign.Val = TableVerticalAlignmentValues.Center;

                            var tcMar = tcPr.GetFirstChild<TableCellMargin>() ?? tcPr.AppendChild(new TableCellMargin());
                            tcMar.GetFirstChild<TopMargin>()?.Remove();
                            tcMar.AppendChild(new TopMargin { Width = "80", Type = TableWidthUnitValues.Dxa });
                            tcMar.GetFirstChild<BottomMargin>()?.Remove();
                            tcMar.AppendChild(new BottomMargin { Width = "80", Type = TableWidthUnitValues.Dxa });

                            // Give the center 'فقط وقدره...' tafqeet cell generous side padding & centered alignment
                            if (gi == 4 && !string.IsNullOrWhiteSpace(cell.InnerText))
                            {
                                tcMar.GetFirstChild<LeftMargin>()?.Remove();
                                tcMar.AppendChild(new LeftMargin { Width = "110", Type = TableWidthUnitValues.Dxa });
                                tcMar.GetFirstChild<RightMargin>()?.Remove();
                                tcMar.AppendChild(new RightMargin { Width = "110", Type = TableWidthUnitValues.Dxa });
                                tcMar.GetFirstChild<StartMargin>()?.Remove();
                                tcMar.AppendChild(new StartMargin { Width = "110", Type = TableWidthUnitValues.Dxa });
                                tcMar.GetFirstChild<EndMargin>()?.Remove();
                                tcMar.AppendChild(new EndMargin { Width = "110", Type = TableWidthUnitValues.Dxa });

                                foreach (var p in cell.Elements<Paragraph>())
                                {
                                    var pPr = p.ParagraphProperties ?? p.PrependChild(new ParagraphProperties());
                                    var jc = pPr.Justification ?? pPr.AppendChild(new Justification());
                                    jc.Val = JustificationValues.Center;
                                    if (pPr.BiDi == null) pPr.AppendChild(new BiDi { Val = true });
                                    NormalizeParagraphPropertiesOrder(pPr);
                                }
                            }

                            NormalizeTableCellMarginOrder(tcMar);
                            NormalizeTableCellPropertiesOrder(tcPr);
                        }

                        // Drop signatures comfortably below the bottom border
                        var sigRow = tRows[sigIdx];
                        var rPr = sigRow.TableRowProperties ?? sigRow.PrependChild(new TableRowProperties());
                        var trHeight = rPr.GetFirstChild<TableRowHeight>() ?? rPr.AppendChild(new TableRowHeight());
                        trHeight.Val = new UInt32Value(680u);
                        trHeight.HeightType = HeightRuleValues.AtLeast;

                        foreach (var cell in sigRow.Elements<TableCell>())
                        {
                            var tcPr = cell.TableCellProperties ?? cell.PrependChild(new TableCellProperties());
                            var tcMar = tcPr.GetFirstChild<TableCellMargin>() ?? tcPr.AppendChild(new TableCellMargin());
                            var topMar = tcMar.GetFirstChild<TopMargin>();
                            if (topMar != null) topMar.Remove();
                            tcMar.AppendChild(new TopMargin { Width = "220", Type = TableWidthUnitValues.Dxa });
                            NormalizeTableCellMarginOrder(tcMar);
                            NormalizeTableCellPropertiesOrder(tcPr);

                            foreach (var p in cell.Elements<Paragraph>())
                            {
                                var pPr = p.ParagraphProperties ?? p.PrependChild(new ParagraphProperties());
                                var spacing = pPr.SpacingBetweenLines ?? pPr.AppendChild(new SpacingBetweenLines());
                                spacing.Before = "220";
                                spacing.After = "0";
                                spacing.Line = "240";
                                spacing.LineRule = LineSpacingRuleValues.Auto;
                                NormalizeParagraphPropertiesOrder(pPr);
                            }
                        }
                    }
                }
            }

            // Shrink any trailing empty paragraphs so Word/LibreOffice never overflows an extra blank page
            foreach (var p in body.Elements<Paragraph>().ToList())
            {
                if (string.IsNullOrWhiteSpace(p.InnerText) && !p.Descendants<Drawing>().Any())
                {
                    var pPr = p.ParagraphProperties ?? p.PrependChild(new ParagraphProperties());
                    var spacing = pPr.SpacingBetweenLines ?? pPr.AppendChild(new SpacingBetweenLines());
                    spacing.Before = "0";
                    spacing.After = "0";
                    spacing.Line = "20"; // 1pt (20 twips)
                    spacing.LineRule = LineSpacingRuleValues.Exact;

                    var rPr = pPr.ParagraphMarkRunProperties ?? pPr.AppendChild(new ParagraphMarkRunProperties());
                    rPr.AppendChild(new FontSize { Val = "2" }); // 1pt
                    NormalizeParagraphPropertiesOrder(pPr);
                }
            }

            doc.MainDocumentPart.Document.Save();
        }

        return ms.ToArray();
    }

    private static void CapFontSize(OpenXmlCompositeElement rPr, int maxHalfPoints)
    {
        var sz = rPr.GetFirstChild<FontSize>();
        if (sz?.Val != null && int.TryParse(sz.Val.Value, out int sv) && sv > maxHalfPoints)
            sz.Val = maxHalfPoints.ToString(CultureInfo.InvariantCulture);

        var szCs = rPr.GetFirstChild<FontSizeComplexScript>();
        if (szCs?.Val != null && int.TryParse(szCs.Val.Value, out int csv) && csv > maxHalfPoints)
            szCs.Val = maxHalfPoints.ToString(CultureInfo.InvariantCulture);
    }

    private static readonly Dictionary<string, int> TcMarOrder = new(StringComparer.Ordinal)
    {
        ["top"] = 1, ["left"] = 2, ["start"] = 3, ["bottom"] = 4, ["right"] = 5, ["end"] = 6
    };

    private static void NormalizeTableCellMarginOrder(TableCellMargin tcMar)
    {
        var children = tcMar.ChildElements.ToList();
        if (children.Count <= 1) return;
        var sorted = children.OrderBy(e => TcMarOrder.GetValueOrDefault(e.LocalName, 99)).ToList();
        if (children.SequenceEqual(sorted)) return;
        tcMar.RemoveAllChildren();
        foreach (var child in sorted) tcMar.AppendChild(child);
    }

    private static readonly Dictionary<string, int> TcPrOrder = new(StringComparer.Ordinal)
    {
        ["cnfStyle"] = 1, ["tcW"] = 2, ["gridSpan"] = 3, ["hMerge"] = 4, ["vMerge"] = 5,
        ["tcBorders"] = 6, ["shd"] = 7, ["noWrap"] = 8, ["tcMar"] = 9, ["textDirection"] = 10,
        ["tcFitText"] = 11, ["vAlign"] = 12, ["hideMark"] = 13
    };

    private static void NormalizeTableCellPropertiesOrder(TableCellProperties tcPr)
    {
        var children = tcPr.ChildElements.ToList();
        if (children.Count <= 1) return;
        var sorted = children.OrderBy(e => TcPrOrder.GetValueOrDefault(e.LocalName, 99)).ToList();
        if (children.SequenceEqual(sorted)) return;
        tcPr.RemoveAllChildren();
        foreach (var child in sorted) tcPr.AppendChild(child);
    }

    private static readonly Dictionary<string, int> PPrOrder = new(StringComparer.Ordinal)
    {
        ["pStyle"] = 1, ["keepNext"] = 2, ["keepLines"] = 3, ["pageBreakBefore"] = 4,
        ["framePr"] = 5, ["widowControl"] = 6, ["numPr"] = 7, ["suppressLineNumbers"] = 8,
        ["pBdr"] = 9, ["shd"] = 10, ["tabs"] = 11, ["suppressAutoHyphens"] = 12,
        ["kinsoku"] = 13, ["wordWrap"] = 14, ["overflowPunct"] = 15, ["topLinePunct"] = 16,
        ["autoSpaceDE"] = 17, ["autoSpaceDN"] = 18, ["bidi"] = 19, ["adjustRightInd"] = 20,
        ["snapToGrid"] = 21, ["spacing"] = 22, ["ind"] = 23, ["contextualSpacing"] = 24,
        ["mirrorIndents"] = 25, ["suppressOverlap"] = 26, ["jc"] = 27, ["textDirection"] = 28,
        ["textAlignment"] = 29, ["textboxTightWrap"] = 30, ["outlineLvl"] = 31,
        ["divId"] = 32, ["cnfStyle"] = 33, ["rPr"] = 34, ["sectPr"] = 35
    };

    private static void NormalizeParagraphPropertiesOrder(ParagraphProperties pPr)
    {
        var children = pPr.ChildElements.ToList();
        if (children.Count <= 1) return;
        var sorted = children.OrderBy(e => PPrOrder.GetValueOrDefault(e.LocalName, 99)).ToList();
        if (children.SequenceEqual(sorted)) return;
        pPr.RemoveAllChildren();
        foreach (var child in sorted) pPr.AppendChild(child);
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
