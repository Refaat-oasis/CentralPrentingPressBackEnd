using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using CentralPress.API.Entities;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace CentralPress.API.Services;

public class WordExportService
{
    private static string SanitizeXml(string? input)
    {
        if (string.IsNullOrEmpty(input)) return string.Empty;
        return new string(input.Where(ch =>
            ch == 0x9 || ch == 0xA || ch == 0xD ||
            (ch >= 0x20 && ch <= 0xD7FF) ||
            (ch >= 0xE000 && ch <= 0xFFFD)).ToArray());
    }

    public static string? ResolveShapeDirectory()
    {
        var candidates = new List<string>
        {
            Path.Combine(AppContext.BaseDirectory, "TasweyaAndMoqaysaShape"),
            Path.Combine(Directory.GetCurrentDirectory(), "TasweyaAndMoqaysaShape"),
            Path.Combine(Directory.GetCurrentDirectory(), "CentralPress.API", "TasweyaAndMoqaysaShape"),
        };

        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        for (int i = 0; i < 6 && dir != null; i++)
        {
            candidates.Add(Path.Combine(dir.FullName, "CentralPress.API", "TasweyaAndMoqaysaShape"));
            candidates.Add(Path.Combine(dir.FullName, "TasweyaAndMoqaysaShape"));
            dir = dir.Parent;
        }

        return candidates.FirstOrDefault(d =>
            Directory.Exists(d) && File.Exists(Path.Combine(d, "moqaysa.docx")));
    }

    public static void EnsureDocxTemplatesExistInShapeDirectory(string? folder = null)
    {
        var targetDir = folder ?? ResolveShapeDirectory();
        if (string.IsNullOrEmpty(targetDir) || !Directory.Exists(targetDir)) return;

        // Converted .docx templates from original .doc shapes must be preserved untouched if present.
        if (File.Exists(Path.Combine(targetDir, "moqaysa.docx")) &&
            File.Exists(Path.Combine(targetDir, "Tasweya1.docx")) &&
            File.Exists(Path.Combine(targetDir, "Tasweya2.docx")))
        {
            return;
        }
    }

    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, byte[]> TemplateCache = new();

    private static byte[] LoadTemplateBytes(string templateFileName)
    {
        return TemplateCache.GetOrAdd(templateFileName, fileName =>
        {
            var shapeDir = ResolveShapeDirectory();
            if (!string.IsNullOrEmpty(shapeDir))
            {
                string fullPath = Path.Combine(shapeDir, fileName);
                if (File.Exists(fullPath))
                    return File.ReadAllBytes(fullPath);

                // Fallback mapping for renamed official templates in TasweyaAndMoqaysaShape
                if (fileName == "Tasweya1.docx" && File.Exists(Path.Combine(shapeDir, "Tasweya_Single.docx")))
                    return File.ReadAllBytes(Path.Combine(shapeDir, "Tasweya_Single.docx"));
                if (fileName == "Tasweya2.docx" && File.Exists(Path.Combine(shapeDir, "Tasweya_Multi.docx")))
                    return File.ReadAllBytes(Path.Combine(shapeDir, "Tasweya_Multi.docx"));
            }

            throw new FileNotFoundException(
                $"تعذر العثور على قالب Word المطلوب ({fileName}) في مجلد TasweyaAndMoqaysaShape.");
        });
    }

    public byte[] ExportMoqaysaToWord(Moqaysa m)
    {
        byte[] templateBytes = LoadTemplateBytes("moqaysa.docx");
        using var mem = new MemoryStream();
        mem.Write(templateBytes, 0, templateBytes.Length);
        mem.Position = 0;

        using (var wordDoc = WordprocessingDocument.Open(mem, true))
        {
            var body = wordDoc.MainDocumentPart?.Document?.Body
                ?? throw new InvalidOperationException("قالب المقايسة غير صالح");

            var tbl = body.Descendants<Table>().First();
            var rows = tbl.Elements<TableRow>().ToList();

            decimal qty = MoqaysaService.ParseQuantity(m.RequiredQuantityText);
            decimal finalUnitPrice = qty > 0m
                ? Math.Round(m.FinalGrandTotal / qty, 2, MidpointRounding.AwayFromZero)
                : m.FinalGrandTotal;

            // Row 0: Replace "xx" placeholder in title with JobOrderNumber
            if (rows.Count > 0)
            {
                ReplaceTokenAcrossCellParagraphs(rows[0].Elements<TableCell>().First(), "xx", SanitizeXml(m.JobOrderNumber));
            }

            // Row 1: Customer, Required Quantity, Unit Price, Specifications
            if (rows.Count > 1)
            {
                FillMoqaysaMetaRow(rows[1].Elements<TableCell>().First(), m, finalUnitPrice);
            }

            // Machinery & Labour list comes strictly from the system's catalog items on this Moqaysa
            var machineries = (m.MachineryItems ?? Array.Empty<MoqaysaMachineryItem>()).ToList();
            var materials = (m.MaterialItems ?? Array.Empty<MoqaysaMaterialItem>()).ToList();

            int workerHeaderIdx = FindRowIndex(rows, "أجور عمال");
            int workerSubtotalIdx = FindRowIndex(rows, "جملة أجور العمال");
            int machineHeaderIdx = FindRowIndex(rows, "مصاريف الآلات");
            int machineSubtotalIdx = FindRowIndex(rows, "جملة م . الآلات");

            if (workerHeaderIdx >= 0 && workerSubtotalIdx > workerHeaderIdx &&
                machineHeaderIdx > workerSubtotalIdx && machineSubtotalIdx > machineHeaderIdx)
            {
                int workerTemplateSlots = workerSubtotalIdx - workerHeaderIdx - 1;
                int neededSlots = Math.Max(workerTemplateSlots, machineries.Count);

                // Expand worker rows if system has more than baseline 11 machines
                if (machineries.Count > workerTemplateSlots)
                {
                    var sampleWorkerRow = rows[workerSubtotalIdx - 1];
                    for (int extra = 0; extra < machineries.Count - workerTemplateSlots; extra++)
                    {
                        tbl.InsertBefore(sampleWorkerRow.CloneNode(true), rows[workerSubtotalIdx]);
                    }
                    rows = tbl.Elements<TableRow>().ToList();
                    workerSubtotalIdx = FindRowIndex(rows, "جملة أجور العمال");
                    machineHeaderIdx = FindRowIndex(rows, "مصاريف الآلات");
                    machineSubtotalIdx = FindRowIndex(rows, "جملة م . الآلات");
                }

                int machineTemplateSlots = machineSubtotalIdx - machineHeaderIdx - 1;
                if (machineries.Count > machineTemplateSlots)
                {
                    var sampleMachineRow = rows[machineSubtotalIdx - 1];
                    for (int extra = 0; extra < machineries.Count - machineTemplateSlots; extra++)
                    {
                        tbl.InsertBefore(sampleMachineRow.CloneNode(true), rows[machineSubtotalIdx]);
                    }
                    rows = tbl.Elements<TableRow>().ToList();
                    workerSubtotalIdx = FindRowIndex(rows, "جملة أجور العمال");
                    machineHeaderIdx = FindRowIndex(rows, "مصاريف الآلات");
                    machineSubtotalIdx = FindRowIndex(rows, "جملة م . الآلات");
                }

                // Fill Materials on Left Side across all rows from workerHeaderIdx to machineSubtotalIdx - 1
                int matCursor = 0;
                void WriteNextMaterialToRow(TableRow row)
                {
                    var cells = row.Elements<TableCell>().ToList();
                    if (cells.Count < 14) return;
                    var mat = matCursor < materials.Count ? materials[matCursor] : null;
                    matCursor++;
                    if (mat != null)
                    {
                        SetCellText(cells[8], SanitizeXml(mat.MaterialNameSnapshot), JustificationValues.Right);
                        SetCellText(cells[9], mat.Quantity > 0 ? mat.Quantity.ToString("G") : "", JustificationValues.Center);
                        SetPoundPiaster(cells[10], cells[11], mat.UnitPrice, blankIfZero: true);
                        SetPoundPiaster(cells[12], cells[13], mat.TotalPrice, blankIfZero: true);
                    }
                    else
                    {
                        ClearRange(cells, 8, 13);
                    }
                }

                // Row 5 (worker header)
                WriteNextMaterialToRow(rows[workerHeaderIdx]);

                // Worker section rows
                for (int i = 0; i < neededSlots; i++)
                {
                    var row = rows[workerHeaderIdx + 1 + i];
                    var cells = row.Elements<TableCell>().ToList();
                    var mac = i < machineries.Count ? machineries[i] : null;
                    bool hasWorker = mac != null && (mac.Hours > 0m || mac.TotalWorkerWages > 0m);

                    SetCellText(cells[0], mac != null ? SanitizeXml(mac.MachineryNameSnapshot) : "", JustificationValues.Right, bold: true);
                    SetCellText(cells[1], mac != null && mac.Hours > 0m ? mac.Hours.ToString("G") : "", JustificationValues.Center);
                    SetPoundPiaster(cells[2], cells[3], hasWorker ? mac!.WorkerHourlyRateSnapshot : 0m, blankIfZero: true);
                    SetPoundPiaster(cells[4], cells[5], hasWorker ? mac!.TotalWorkerWages : 0m, blankIfZero: true);
                    SetCellText(cells[6], "", JustificationValues.Center);
                    SetCellText(cells[7], "", JustificationValues.Center);
                    WriteNextMaterialToRow(row);
                }

                // جملة أجور العمال (placed in كلي C6, C7)
                var wSubCells = rows[workerSubtotalIdx].Elements<TableCell>().ToList();
                SetPoundPiaster(wSubCells[6], wSubCells[7], m.TotalWorkerWages, bold: true, blankIfZero: m.TotalWorkerWages <= 0m);
                WriteNextMaterialToRow(rows[workerSubtotalIdx]);

                // مصاريف الآلات header
                WriteNextMaterialToRow(rows[machineHeaderIdx]);

                // Machine operating rows
                for (int i = 0; i < neededSlots; i++)
                {
                    var row = rows[machineHeaderIdx + 1 + i];
                    var cells = row.Elements<TableCell>().ToList();
                    var mac = i < machineries.Count ? machineries[i] : null;
                    decimal mHours = mac != null ? (mac.MachineHours > 0m ? mac.MachineHours : mac.Hours) : 0m;
                    bool hasMachine = mac != null && (mHours > 0m || mac.TotalMachineOperatingCost > 0m);

                    SetCellText(cells[0], mac != null ? SanitizeXml(mac.MachineryNameSnapshot) : "", JustificationValues.Right, bold: true);
                    SetCellText(cells[1], mHours > 0m ? mHours.ToString("G") : "", JustificationValues.Center);
                    SetPoundPiaster(cells[2], cells[3], hasMachine ? mac!.MachineHourlyRateSnapshot : 0m, blankIfZero: true);
                    SetPoundPiaster(cells[4], cells[5], hasMachine ? mac!.TotalMachineOperatingCost : 0m, blankIfZero: true);
                    SetCellText(cells[6], "", JustificationValues.Center);
                    SetCellText(cells[7], "", JustificationValues.Center);
                    WriteNextMaterialToRow(row);
                }

                // جملة م . الآلات (in كلي C6, C7) & الجملة : (in Left Box C11, C12)
                var mSubCells = rows[machineSubtotalIdx].Elements<TableCell>().ToList();
                SetPoundPiaster(mSubCells[6], mSubCells[7], m.TotalMachineCosts, bold: true, blankIfZero: m.TotalMachineCosts <= 0m);
                if (mSubCells.Count >= 13)
                    SetPoundPiaster(mSubCells[11], mSubCells[12], m.RawMaterials, bold: true, blankIfZero: m.RawMaterials <= 0m);

                // Cascade rows below جملة م . الآلات
                int cur = machineSubtotalIdx + 1;
                if (cur + 15 < rows.Count)
                {
                    // الخامات : in جزئي (C4, C5)
                    var cRaw = rows[cur++].Elements<TableCell>().ToList();
                    SetPoundPiaster(cRaw[4], cRaw[5], m.RawMaterials, blankIfZero: m.RawMaterials <= 0m);

                    // م. تشغيل الخامات in جزئي (C4, C5) & المرتجع in C11, C12
                    var cMatProc = rows[cur++].Elements<TableCell>().ToList();
                    SetPoundPiaster(cMatProc[4], cMatProc[5], m.MaterialProcessingFee, blankIfZero: m.MaterialProcessingFee <= 0m);
                    if (cMatProc.Count >= 13) SetPoundPiaster(cMatProc[11], cMatProc[12], 0m, showDashIfZero: true);

                    // م. تشغيل الأفلام in جزئي (C4, C5)
                    var cFilm = rows[cur++].Elements<TableCell>().ToList();
                    SetPoundPiaster(cFilm[4], cFilm[5], m.FilmsProcessingFee, blankIfZero: m.FilmsProcessingFee <= 0m);

                    // جملة الخامات in كلي (C6, C7) & صافى الجملة in C11, C12
                    var cTotMat = rows[cur++].Elements<TableCell>().ToList();
                    SetPoundPiaster(cTotMat[6], cTotMat[7], m.TotalMaterials, bold: true);
                    if (cTotMat.Count >= 13) SetPoundPiaster(cTotMat[11], cTotMat[12], m.RawMaterials, bold: true, blankIfZero: m.RawMaterials <= 0m);

                    // All remaining waterfall steps in كلي (C6, C7)
                    SetWaterfallRow(rows[cur++], m.SubTotal1, bold: true);
                    SetWaterfallRow(rows[cur++], m.AdminExpenses, showDashIfZero: !m.ApplyAdminExpenses);
                    SetWaterfallRow(rows[cur++], m.TotalExpenses, bold: true);
                    SetWaterfallRow(rows[cur++], m.MarkupAmount, showDashIfZero: !m.ApplyMarkup);
                    SetWaterfallRow(rows[cur++], m.TotalAfterMarkup, bold: true);
                    SetWaterfallRow(rows[cur++], m.MinistryOfFinanceFee);
                    SetWaterfallRow(rows[cur++], m.WorkersSyndicateFund);
                    SetWaterfallRow(rows[cur++], m.SubTotal2, bold: true);
                    SetWaterfallRow(rows[cur++], m.OtherAmount, showDashIfZero: !m.ApplyOther);
                    SetWaterfallRow(rows[cur++], m.SubTotal3, bold: true);
                    SetWaterfallRow(rows[cur++], m.VatAmount, showDashIfZero: !(m.ApplyVat || m.VatAmount > 0m));
                    SetWaterfallRow(rows[cur], m.FinalGrandTotal, bold: true);
                }
            }

            NormalizeDocumentOpenXml(body);
            wordDoc.MainDocumentPart!.Document.Save();
        }

        return mem.ToArray();
    }

    public byte[] ExportTasweyaToWord(Tasweya t)
    {
        var sharesAcrossMoqaysat = t.MoqaysaLinks?
            .Where(ml => ml.Moqaysa != null && ml.Moqaysa.AccountShares != null && ml.Moqaysa.AccountShares.Any())
            .GroupBy(ml => ml.MoqaysaId)
            .Select(g => g.First())
            .SelectMany(ml => ml.Moqaysa!.AccountShares)
            .GroupBy(s => s.AccountId)
            .Select(g =>
            {
                var first = g.First();
                var accName = first.Account?.AccountNameAr
                    ?? (first.AccountId == t.AccountId ? (t.Account?.AccountNameAr ?? "غير محدد") : $"حساب #{first.AccountId}");
                var totalAllocated = g.Sum(x => x.AllocatedAmount);
                return (Name: accName, Amount: totalAllocated > 0 ? totalAllocated : t.SettlementGrandTotal);
            }).ToList() ?? new List<(string Name, decimal Amount)>();

        bool isMultiAccount = sharesAcrossMoqaysat.Count > 1;
        if (isMultiAccount)
        {
            decimal sharesSum = sharesAcrossMoqaysat.Sum(s => s.Amount);
            if (sharesSum != t.SettlementGrandTotal && sharesSum > 0m)
            {
                var last = sharesAcrossMoqaysat[^1];
                sharesAcrossMoqaysat[^1] = (last.Name, Math.Round(last.Amount + (t.SettlementGrandTotal - sharesSum), 2, MidpointRounding.AwayFromZero));
            }
        }

        byte[] templateBytes = LoadTemplateBytes(isMultiAccount ? "Tasweya2.docx" : "Tasweya1.docx");
        using var mem = new MemoryStream();
        mem.Write(templateBytes, 0, templateBytes.Length);
        mem.Position = 0;

        using (var wordDoc = WordprocessingDocument.Open(mem, true))
        {
            var body = wordDoc.MainDocumentPart?.Document?.Body
                ?? throw new InvalidOperationException("قالب التسوية غير صالح");

            var tbl = body.Descendants<Table>().First();
            var rows = tbl.Elements<TableRow>().ToList();

            // Row 0: Header Settlement Number & Date
            if (rows.Count > 0)
            {
                var headerCell = rows[0].Elements<TableCell>().First();
                var numPara = headerCell.Elements<Paragraph>()
                    .FirstOrDefault(p => p.InnerText.Contains("placeHolder") || p.InnerText.Contains("رقم التسوية"));
                if (numPara != null)
                {
                    ReplaceParagraphTextPreservingStyle(
                        numPara,
                        $"رقم التسوية : {SanitizeXml(t.SettlementNumber)}          بتاريخ : {t.SettlementDate:yyyy/MM/dd} م",
                        JustificationValues.Center);
                }
            }

            // Replace R3 multi-paragraph block with aligned 9-column rows preserving exact R3 borders/widths
            if (rows.Count > 4)
            {
                var templateBodyRow = rows[3];
                var entries = BuildTasweyaLines(t, sharesAcrossMoqaysat, isMultiAccount);

                for (int i = 0; i < entries.Count; i++)
                {
                    var line = entries[i];
                    var newRow = (TableRow)templateBodyRow.CloneNode(true);
                    var cells = newRow.Elements<TableCell>().ToList();

                    bool isFirst = i == 0;
                    bool isLast = i == entries.Count - 1;

                    for (int ci = 0; ci < cells.Count; ci++)
                    {
                        var tcPr = cells[ci].TableCellProperties;
                        var borders = tcPr?.GetFirstChild<TableCellBorders>();
                        if (borders != null)
                        {
                            if (!isFirst) borders.TopBorder = new TopBorder { Val = BorderValues.None };
                            if (!isLast) borders.BottomBorder = new BottomBorder { Val = BorderValues.None };
                        }
                    }

                    // C0,C1 = Right اجمالى; C2,C3 = Right أفرادى; C4 = البيان; C5,C6 = Inner Left اجمالى; C7,C8 = Far Left أفرادى
                    SetSingleLineCell(cells[0], cells[1], line.RightTotal, JustificationValues.Center, line.Bold);
                    SetSingleLineRawPair(cells[2], cells[3], line.RightPartialText, line.RightPartialAmount, line.Bold);
                    SetCellText(cells[4], SanitizeXml(line.Statement), JustificationValues.Right, line.Bold, line.Underline);
                    SetSingleLineCell(cells[5], cells[6], line.LeftInnerTotal, JustificationValues.Center, line.Bold);
                    SetSingleLineCell(cells[7], cells[8], line.LeftOuterPartial, JustificationValues.Center, line.Bold);

                    tbl.InsertBefore(newRow, templateBodyRow);
                }

                templateBodyRow.Remove();

                // R4 (now shifted): Grand Total & Tafqeet row (Right-aligned in البيان per Item 2)
                rows = tbl.Elements<TableRow>().ToList();
                var grandRow = rows[rows.Count - 2];
                var gCells = grandRow.Elements<TableCell>().ToList();
                string cleanWords = ArabicTafqeetService.StripLeadingPrefix(t.TotalAmountInWordsAr);

                SetPoundPiaster(gCells[0], gCells[1], t.SettlementGrandTotal, bold: true);
                SetCellText(gCells[4], SanitizeXml($"فقط وقدره {cleanWords}"), JustificationValues.Right, bold: true);
                SetPoundPiaster(gCells[7], gCells[8], t.SettlementGrandTotal, bold: true);
            }

            NormalizeDocumentOpenXml(body);
            wordDoc.MainDocumentPart!.Document.Save();
        }

        return mem.ToArray();
    }

    public byte[] ExportPaymentOrderToWord(PaymentOrder p)
    {
        using var mem = new MemoryStream();
        using (var wordDoc = WordprocessingDocument.Create(mem, WordprocessingDocumentType.Document, true))
        {
            var mainPart = wordDoc.AddMainDocumentPart();
            mainPart.Document = new Document();
            var body = mainPart.Document.AppendChild(new Body());

            var secPr = new SectionProperties();
            var pgSz = new PageSize() { Width = 11906U, Height = 16838U, Orient = PageOrientationValues.Portrait };
            var pgMar = new PageMargin() { Top = 720, Right = 720, Bottom = 720, Left = 720 };
            secPr.Append(pgSz, pgMar, new BiDi());

            var pHeader = new Paragraph(
                new ParagraphProperties(new Justification() { Val = JustificationValues.Center }, new BiDi()),
                new Run(
                    new RunProperties(new Bold(), new FontSize() { Val = "26" }, new Color() { Val = "1A365D" }, new RightToLeftText()),
                    new Text(SanitizeXml("جامعة أسيوط - مطبعة الجامعة المركزية (A.U.P)")) { Space = SpaceProcessingModeValues.Preserve }),
                new Break(),
                new Run(
                    new RunProperties(new Bold(), new FontSize() { Val = "30" }, new RightToLeftText()),
                    new Text(SanitizeXml("أمر دفع واعتماد سداد رسمي")) { Space = SpaceProcessingModeValues.Preserve }));
            body.Append(pHeader);

            var pMeta = new Paragraph(
                new ParagraphProperties(new BiDi()),
                new Run(
                    new RunProperties(new Bold(), new FontSize() { Val = "24" }, new RightToLeftText()),
                    new Text(SanitizeXml($"رقم أمر الدفع: {p.PaymentOrderNumber} | التاريخ: {p.PaymentDate:yyyy/MM/dd}\n")) { Space = SpaceProcessingModeValues.Preserve }),
                new Run(
                    new RunProperties(new FontSize() { Val = "22" }, new RightToLeftText()),
                    new Text(SanitizeXml($"الحساب: {p.Account?.AccountNameAr} | حالة السداد: {(p.IsConfirmed ? "مؤكد ومسدد رسمياً" : "مسودة غير مسددة")}")) { Space = SpaceProcessingModeValues.Preserve }));
            body.Append(pMeta);

            var table = new Table();
            table.AppendChild(new TableProperties(
                new TableBorders(
                    new TopBorder() { Val = BorderValues.Single, Size = 12 },
                    new BottomBorder() { Val = BorderValues.Single, Size = 12 },
                    new LeftBorder() { Val = BorderValues.Single, Size = 12 },
                    new RightBorder() { Val = BorderValues.Single, Size = 12 },
                    new InsideHorizontalBorder() { Val = BorderValues.Single, Size = 6 },
                    new InsideVerticalBorder() { Val = BorderValues.Single, Size = 6 }),
                new TableWidth() { Type = TableWidthUnitValues.Pct, Width = "5000" },
                new TableJustification() { Val = TableRowAlignmentValues.Center },
                new BiDiVisual()));

            table.AppendChild(CreateTwoColRow("كشف التسوية / رقم المطالبة", "المبلغ المسدد (جنيه)", isHeader: true));
            foreach (var link in p.TasweyaLinks)
            {
                table.AppendChild(CreateTwoColRow(
                    $"تسوية رقم {link.Tasweya?.SettlementNumber} (مطالبة {link.Tasweya?.ClaimNumber} - {link.Tasweya?.Customer?.CustomerNameAr})",
                    $"{link.SnapshotTasweyaAmount:F2}"));
            }
            table.AppendChild(CreateTwoColRow("إجمالي المبلغ المسدد بأمر الدفع", $"{p.TotalPaidAmount:F2}", isBold: true));
            body.Append(table);

            var pTafqeet = new Paragraph(
                new ParagraphProperties(new BiDi(), new SpacingBetweenLines() { Before = "360" }),
                new Run(
                    new RunProperties(new Bold(), new FontSize() { Val = "24" }, new RightToLeftText()),
                    new Text(SanitizeXml($"المبلغ بالحروف: {p.TotalAmountInWordsAr}")) { Space = SpaceProcessingModeValues.Preserve }));
            body.Append(pTafqeet);

            var pSig = new Paragraph(
                new ParagraphProperties(new BiDi(), new SpacingBetweenLines() { Before = "720" }),
                new Run(
                    new RunProperties(new Bold(), new FontSize() { Val = "22" }, new RightToLeftText()),
                    new Text(SanitizeXml("المحاسب المسؤول: .....................          مدير الحسابات: .....................          اعتماد المدير المالي: .....................")) { Space = SpaceProcessingModeValues.Preserve }));
            body.Append(pSig);

            body.Append(secPr);
            NormalizeDocumentOpenXml(body);
            mainPart.Document.Save();
        }
        return mem.ToArray();
    }

    private sealed record TasweyaLineEntry(
        string Statement,
        decimal? RightTotal = null,
        decimal? RightPartialAmount = null,
        (string Pi, string Po)? RightPartialText = null,
        decimal? LeftInnerTotal = null,
        decimal? LeftOuterPartial = null,
        bool Bold = false,
        bool Underline = false);

    private static List<TasweyaLineEntry> BuildTasweyaLines(
        Tasweya t,
        List<(string Name, decimal Amount)> sharesAcrossMoqaysat,
        bool isMultiAccount)
    {
        var list = new List<TasweyaLineEntry>();
        if (isMultiAccount)
        {
            list.Add(new TasweyaLineEntry("من مذكورين", Bold: true, Underline: true));
            foreach (var share in sharesAcrossMoqaysat)
            {
                list.Add(new TasweyaLineEntry($"    من حـ / {share.Name}", RightPartialAmount: share.Amount, Bold: true));
            }
        }
        else
        {
            list.Add(new TasweyaLineEntry(
                t.Account?.DebitHeaderTextAr ?? "من حـ / الإدارة العامة",
                RightPartialAmount: t.SettlementGrandTotal,
                Bold: true,
                Underline: true));
        }

        list.Add(new TasweyaLineEntry(
            $"إذن تشغيل رقم : {t.JobOrderNumbersText}",
            RightTotal: t.SettlementGrandTotal,
            RightPartialText: ("ـــــ", "ــــــــ"),
            Bold: true));
        list.Add(new TasweyaLineEntry($"الجهة : {t.Customer?.CustomerNameAr}"));
        list.Add(new TasweyaLineEntry($"مطالبة رقم : {t.ClaimNumber}"));
        list.Add(new TasweyaLineEntry("إلى مذكورين", Bold: true, Underline: true));
        list.Add(new TasweyaLineEntry("حـ / الإيرادات", LeftOuterPartial: t.RevenuesTotal, Bold: true, Underline: true));

        if (t.VatTotal > 0m)
        {
            list.Add(new TasweyaLineEntry("حـ / القيمة المضافة", LeftOuterPartial: t.VatTotal, Bold: true, Underline: true));
        }

        list.Add(new TasweyaLineEntry("خامـــــــات", LeftInnerTotal: t.TotalMaterialsSum));
        list.Add(new TasweyaLineEntry("أجور عمال", LeftInnerTotal: t.TotalWorkerWagesSum));
        list.Add(new TasweyaLineEntry("م . الآلات", LeftInnerTotal: t.TotalMachineCostsSum));
        if (t.AdminExpensesSum > 0m) list.Add(new TasweyaLineEntry("م . إدارية", LeftInnerTotal: t.AdminExpensesSum));
        if (t.MarkupAmountSum > 0m) list.Add(new TasweyaLineEntry("النسبة المضافة", LeftInnerTotal: t.MarkupAmountSum));
        if (t.MinistryOfFinanceSum > 0m) list.Add(new TasweyaLineEntry("وزارة المالية", LeftInnerTotal: t.MinistryOfFinanceSum));
        if (t.WorkersFundSum > 0m) list.Add(new TasweyaLineEntry("ص. ت. العاملين", LeftInnerTotal: t.WorkersFundSum));
        if (t.OtherAmountSum > 0m) list.Add(new TasweyaLineEntry("أخرى", LeftInnerTotal: t.OtherAmountSum));
        list.Add(new TasweyaLineEntry("إجمالي بنود الإيرادات", LeftInnerTotal: t.RevenuesTotal, Bold: true));

        return list;
    }

    private static int FindRowIndex(List<TableRow> rows, string containsText)
    {
        for (int i = 0; i < rows.Count; i++)
        {
            var firstCell = rows[i].Elements<TableCell>().FirstOrDefault();
            if (firstCell != null && firstCell.InnerText.Contains(containsText))
                return i;
        }
        return -1;
    }

    private static void SetWaterfallRow(TableRow row, decimal amount, bool bold = false, bool showDashIfZero = false)
    {
        var cells = row.Elements<TableCell>().ToList();
        if (cells.Count < 8) return;
        SetPoundPiaster(cells[6], cells[7], amount, bold: bold, showDashIfZero: showDashIfZero);
    }

    private static void SetSingleLineCell(TableCell piCell, TableCell poCell, decimal? amount, JustificationValues align, bool bold)
    {
        if (!amount.HasValue)
        {
            SetCellText(piCell, "", align);
            SetCellText(poCell, "", align);
            return;
        }
        SetPoundPiaster(piCell, poCell, amount.Value, bold: bold);
    }

    private static void SetSingleLineRawPair(TableCell piCell, TableCell poCell, (string Pi, string Po)? rawPair, decimal? amount, bool bold)
    {
        if (rawPair.HasValue)
        {
            SetCellText(piCell, rawPair.Value.Pi, JustificationValues.Center, bold);
            SetCellText(poCell, rawPair.Value.Po, JustificationValues.Center, bold);
            return;
        }
        SetSingleLineCell(piCell, poCell, amount, JustificationValues.Center, bold);
    }

    private static void SetPoundPiaster(TableCell piCell, TableCell poCell, decimal amount, bool bold = false, bool blankIfZero = false, bool showDashIfZero = false)
    {
        bool isZero = Math.Abs(amount) < 0.001m;
        if (isZero)
        {
            if (blankIfZero)
            {
                SetCellText(piCell, "", JustificationValues.Center);
                SetCellText(poCell, "", JustificationValues.Center);
                return;
            }
            if (showDashIfZero)
            {
                SetCellText(piCell, "-", JustificationValues.Center, bold);
                SetCellText(poCell, "-", JustificationValues.Center, bold);
                return;
            }
        }

        long totalPiasters = (long)Math.Round(Math.Abs(amount) * 100m, MidpointRounding.AwayFromZero);
        long p = totalPiasters / 100L;
        long pi = totalPiasters % 100L;

        string piText = (pi == 0 && p > 0) ? "-" : pi.ToString("D2");
        SetCellText(piCell, piText, JustificationValues.Center, bold);
        SetCellText(poCell, p.ToString("N0"), JustificationValues.Center, bold);
    }

    private static void ClearRange(List<TableCell> cells, int startIdx, int endIdx)
    {
        for (int i = startIdx; i <= endIdx && i < cells.Count; i++)
        {
            SetCellText(cells[i], "", JustificationValues.Center);
        }
    }

    private static void SetCellText(TableCell cell, string text, JustificationValues align, bool bold = false, bool underline = false)
    {
        var paras = cell.Elements<Paragraph>().ToList();
        var firstPara = paras.FirstOrDefault() ?? cell.AppendChild(new Paragraph());
        for (int i = 1; i < paras.Count; i++)
            paras[i].Remove();

        ReplaceParagraphTextPreservingStyle(firstPara, text, align, bold, underline);
    }

    private static void ReplaceParagraphTextPreservingStyle(
        Paragraph para,
        string text,
        JustificationValues? align = null,
        bool? bold = null,
        bool? underline = null)
    {
        var pPr = para.GetFirstChild<ParagraphProperties>();
        if (pPr == null)
        {
            pPr = new ParagraphProperties();
            para.PrependChild(pPr);
        }
        if (align.HasValue)
        {
            pPr.RemoveAllChildren<Justification>();
            pPr.Append(new Justification { Val = align.Value });
        }
        if (pPr.GetFirstChild<BiDi>() == null)
            pPr.Append(new BiDi());

        var templateRPr = para.Descendants<Run>()
            .Select(r => r.GetFirstChild<RunProperties>())
            .FirstOrDefault(rp => rp != null)?.CloneNode(true) as RunProperties
            ?? new RunProperties(new FontSize { Val = "18" }, new RightToLeftText());

        if (bold == true && templateRPr.GetFirstChild<Bold>() == null)
            templateRPr.Append(new Bold());
        if (underline == true && templateRPr.GetFirstChild<Underline>() == null)
            templateRPr.Append(new Underline { Val = UnderlineValues.Single });

        foreach (var child in para.Elements().Where(e => e is not ParagraphProperties).ToList())
            child.Remove();

        para.Append(new Run(templateRPr, new Text(SanitizeXml(text)) { Space = SpaceProcessingModeValues.Preserve }));
    }

    private static void ReplaceTokenAcrossCellParagraphs(TableCell cell, string token, string replacement)
    {
        foreach (var para in cell.Elements<Paragraph>())
        {
            if (para.InnerText.Contains(token))
            {
                string updated = para.InnerText.Replace(token, replacement);
                ReplaceParagraphTextPreservingStyle(para, updated);
                return;
            }
        }
    }

    private static void FillMoqaysaMetaRow(TableCell cell, Moqaysa m, decimal unitPrice)
    {
        var paras = cell.Elements<Paragraph>().ToList();
        string custName = SanitizeXml(m.Customer?.CustomerNameAr ?? "................");
        string qtyText = SanitizeXml(m.RequiredQuantityText);
        string specs = SanitizeXml(m.SpecificationsText);

        string line1 = $"الجهة الطالبة : {custName}          الكمية المطلوبة : {qtyText}          سعر الوحدة : {unitPrice:N2} ج.م";
        string line2 = $"المواصفــــات : {specs}";

        var p0 = paras.FirstOrDefault() ?? cell.AppendChild(new Paragraph());
        ReplaceParagraphTextPreservingStyle(p0, line1, JustificationValues.Right, bold: true);

        if (paras.Count > 1)
        {
            ReplaceParagraphTextPreservingStyle(paras[1], line2, JustificationValues.Right);
            for (int i = 2; i < paras.Count; i++) paras[i].Remove();
        }
        else
        {
            var p1 = (Paragraph)p0.CloneNode(true);
            ReplaceParagraphTextPreservingStyle(p1, line2, JustificationValues.Right);
            cell.Append(p1);
        }
    }

    private static void NormalizeDocumentOpenXml(Body body)
    {
        foreach (var tbl in body.Descendants<Table>())
        {
            var tblPr = tbl.GetFirstChild<TableProperties>();
            if (tblPr == null)
            {
                tblPr = new TableProperties();
                tbl.PrependChild(tblPr);
            }
            if (tblPr.GetFirstChild<BiDiVisual>() == null)
            {
                tblPr.Append(new BiDiVisual());
            }
        }

        foreach (var tNode in body.Descendants<Text>())
        {
            tNode.Text = SanitizeXml(tNode.Text);
            tNode.Space = SpaceProcessingModeValues.Preserve;
        }
    }

    private static TableRow CreateTwoColRow(string label, string val, bool isHeader = false, bool isBold = false)
    {
        var row = new TableRow();
        row.Append(
            CreateSimpleCell(label, "3500", isHeader, isBold, JustificationValues.Right),
            CreateSimpleCell(val, "1500", isHeader, isBold, JustificationValues.Center));
        return row;
    }

    private static TableCell CreateSimpleCell(string text, string widthPct, bool isHeader, bool isBold, JustificationValues align) =>
        new(
            new TableCellProperties(new TableCellWidth() { Type = TableWidthUnitValues.Pct, Width = widthPct }),
            new Paragraph(
                new ParagraphProperties(new Justification() { Val = align }, new BiDi()),
                new Run(
                    new RunProperties(
                        isHeader || isBold ? new Bold() : new RunProperties(),
                        new FontSize() { Val = isHeader ? "24" : "20" },
                        new RightToLeftText()),
                    new Text(SanitizeXml(text)) { Space = SpaceProcessingModeValues.Preserve })));
}
