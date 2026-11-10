using System;
using System.IO;
using System.Linq;
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

    private static void AppendDocumentHeaderWithFallback(Body body, string titleText)
    {
        string logoPath = Path.Combine(AppContext.BaseDirectory, "Assets", "press_logo.png");
        bool _ = File.Exists(logoPath); // Checked safely without unhandled exception

        var pHeader = new Paragraph(
            new ParagraphProperties(new Justification() { Val = JustificationValues.Center }, new BiDi()),
            new Run(
                new RunProperties(new Bold(), new FontSize() { Val = "26" }, new Color() { Val = "1A365D" }, new RightToLeftText()),
                new Text(SanitizeXml("جامعة أسيوط - مطبعة الجامعة المركزية (A.U.P)")) { Space = SpaceProcessingModeValues.Preserve }
            ),
            new Break(),
            new Run(
                new RunProperties(new Bold(), new FontSize() { Val = "32" }, new RightToLeftText()),
                new Text(SanitizeXml(titleText)) { Space = SpaceProcessingModeValues.Preserve }
            )
        );
        body.Append(pHeader);
    }

    public byte[] ExportMoqaysaToWord(Moqaysa m)
    {
        using var mem = new MemoryStream();
        using (var wordDoc = WordprocessingDocument.Create(mem, WordprocessingDocumentType.Document, true))
        {
            var mainPart = wordDoc.AddMainDocumentPart();
            mainPart.Document = new Document();
            var body = mainPart.Document.AppendChild(new Body());

            // Section Properties: A4 Portrait + RTL
            var secPr = new SectionProperties();
            var pgSz = new PageSize() { Width = 11906U, Height = 16838U, Orient = PageOrientationValues.Portrait }; // A4 in dxa
            var pgMar = new PageMargin() { Top = 720, Right = 720, Bottom = 720, Left = 720 }; // 0.5 inch margins
            var bidi = new BiDi();
            secPr.Append(pgSz, pgMar, bidi);

            // Title Header with Safe Fallback (MED-14)
            AppendDocumentHeaderWithFallback(body, $"مقايسة لإذن تشغيل رقم ( {m.JobOrderNumber} )");

            decimal qty = MoqaysaService.ParseQuantity(m.RequiredQuantityText);
            decimal finalUnitPrice = qty > 0m
                ? Math.Round(m.FinalGrandTotal / qty, 2, MidpointRounding.AwayFromZero)
                : m.FinalGrandTotal;

            // Customer & Specifications (MED-07: Unit price included)
            var pMeta = new Paragraph(
                new ParagraphProperties(new BiDi()),
                new Run(
                    new RunProperties(new Bold(), new FontSize() { Val = "22" }, new RightToLeftText()),
                    new Text(SanitizeXml($"الجهة الطالبة: {m.Customer?.CustomerNameAr ?? "غير محدد"} | الكمية المطلوبة: {m.RequiredQuantityText} | سعر النسخة: {finalUnitPrice:N2} ج.م شامل ض.ق.م")) { Space = SpaceProcessingModeValues.Preserve }
                )
            );
            body.Append(pMeta);

            if (!string.IsNullOrWhiteSpace(m.SpecificationsText))
            {
                var pSpecs = new Paragraph(
                    new ParagraphProperties(new BiDi()),
                    new Run(
                        new RunProperties(new FontSize() { Val = "20" }, new RightToLeftText()),
                        new Text(SanitizeXml($"المواصفات: {m.SpecificationsText}")) { Space = SpaceProcessingModeValues.Preserve }
                    )
                );
                body.Append(pSpecs);
            }

            // Summary Table
            var table = new Table();
            var tblPr = new TableProperties(
                new TableBorders(
                    new TopBorder() { Val = BorderValues.Single, Size = 12 },
                    new BottomBorder() { Val = BorderValues.Single, Size = 12 },
                    new LeftBorder() { Val = BorderValues.Single, Size = 12 },
                    new RightBorder() { Val = BorderValues.Single, Size = 12 },
                    new InsideHorizontalBorder() { Val = BorderValues.Single, Size = 6 },
                    new InsideVerticalBorder() { Val = BorderValues.Single, Size = 6 }
                ),
                new TableWidth() { Type = TableWidthUnitValues.Pct, Width = "5000" },
                new TableJustification() { Val = TableRowAlignmentValues.Center },
                new BiDiVisual()
            );
            table.AppendChild(tblPr);

            // Add Header Rows
            table.AppendChild(CreateCascadeWordRow("عناصر المقايسة والحساب", "جزئي", "كلي", "الخامــــــــــــات", isHeader: true));
            table.AppendChild(CreateCascadeWordRow("البيان", "ق / جنيه", "ق / جنيه", "الصنف / الكمية / السعر", isHeader: true));

            // Section 1: Worker Wages Subtotal
            table.AppendChild(CreateCascadeWordRow("جملة أجور العمال", null, m.TotalWorkerWages, isBold: true));

            // Section 2: Machine Operating Costs Subtotal
            table.AppendChild(CreateCascadeWordRow("جملة مصاريف الآلات", null, m.TotalMachineCosts, isBold: true));

            // Cascade 1: Raw Materials
            table.AppendChild(CreateCascadeWordRow("الخامات الأولية", m.RawMaterials, null, $"إجمالي الخامات: {m.RawMaterials:F2} ج.م"));

            // Cascade 2: Material Processing Fee
            table.AppendChild(CreateCascadeWordRow("م. تشغيل الخامات (5.5%)", m.MaterialProcessingFee, null));

            // Cascade 3: Films
            if (m.FilmsProcessingFee > 0)
                table.AppendChild(CreateCascadeWordRow("م. تشغيل الأفلام", m.FilmsProcessingFee, null));

            // Cascade 4: Total Materials
            table.AppendChild(CreateCascadeWordRow("جملة الخامات", null, m.TotalMaterials, isBold: true));

            // Cascade 5: Subtotal 1
            table.AppendChild(CreateCascadeWordRow("الإجمالي (1)", null, m.SubTotal1, isBold: true));

            // Cascade 6: Admin Expenses
            table.AppendChild(CreateCascadeWordRow($"مصروفات إدارية {(m.ApplyAdminExpenses ? "(8.5%)" : "")}", m.ApplyAdminExpenses ? m.AdminExpenses : null, null));

            // Cascade 7: Total Expenses
            table.AppendChild(CreateCascadeWordRow("إجمالي المصروفات", null, m.TotalExpenses, isBold: true));

            // Cascade 8: Markup
            table.AppendChild(CreateCascadeWordRow($"النسبة {(m.ApplyMarkup ? $"({m.SelectedMarkupPercent}%)" : "")}", m.ApplyMarkup ? m.MarkupAmount : null, null));

            // Cascade 9: Total After Markup
            table.AppendChild(CreateCascadeWordRow("الجملة", null, m.TotalAfterMarkup, isBold: true));

            // Cascade 10: Ministry of Finance
            table.AppendChild(CreateCascadeWordRow("وزارة المالية (15%)", m.MinistryOfFinanceFee, null));

            // Cascade 11: Workers Fund
            table.AppendChild(CreateCascadeWordRow("ص. ت. العاملين (10%)", m.WorkersSyndicateFund, null));

            // Cascade 12: Subtotal 2
            table.AppendChild(CreateCascadeWordRow("الإجمالي (2)", null, m.SubTotal2, isBold: true));

            // Cascade 13: Other
            if (m.ApplyOther)
                table.AppendChild(CreateCascadeWordRow("أخرى (10%)", m.OtherAmount, null));

            // Cascade 14: Subtotal 3
            table.AppendChild(CreateCascadeWordRow("إجمالي (3)", null, m.SubTotal3, isBold: true));

            // Cascade 15: VAT
            if (m.ApplyVat || m.VatAmount > 0)
                table.AppendChild(CreateCascadeWordRow("ض. القيمة المضافة (14%)", m.VatAmount, null));

            // Final Grand Total & Unit Price
            table.AppendChild(CreateCascadeWordRow("القيمة الإجمالية النهائية", null, m.FinalGrandTotal, isBold: true));
            table.AppendChild(CreateCascadeWordRow("سعر النسخة النهائي (شامل ض.ق.م)", null, finalUnitPrice, isBold: true));

            body.Append(table);

            // HIGH-18: Detailed Materials Breakdown Table
            var pMatTitle = new Paragraph(
                new ParagraphProperties(new BiDi(), new SpacingBetweenLines() { Before = "360", After = "120" }),
                new Run(new RunProperties(new Bold(), new FontSize() { Val = "24" }, new RightToLeftText()),
                    new Text(SanitizeXml("أولاً: بيان تفصيلي بالخامات المطلوبة للتشغيل")) { Space = SpaceProcessingModeValues.Preserve })
            );
            body.Append(pMatTitle);

            var matTable = new Table();
            matTable.AppendChild(new TableProperties(
                new TableBorders(
                    new TopBorder() { Val = BorderValues.Single, Size = 6 },
                    new BottomBorder() { Val = BorderValues.Single, Size = 6 },
                    new InsideHorizontalBorder() { Val = BorderValues.Single, Size = 4 },
                    new InsideVerticalBorder() { Val = BorderValues.Single, Size = 4 }
                ),
                new TableWidth() { Type = TableWidthUnitValues.Pct, Width = "5000" },
                new TableJustification() { Val = TableRowAlignmentValues.Center },
                new BiDiVisual()
            ));

            matTable.AppendChild(CreateCascadeWordRow("م", "إسم الصنف / الخامة", "الكمية", "سعر الوحدة", isHeader: true));
            int mIdx = 1;
            foreach (var mat in m.MaterialItems ?? Enumerable.Empty<MoqaysaMaterialItem>())
            {
                matTable.AppendChild(CreateCascadeWordRow(
                    $"{mIdx}. {mat.MaterialNameSnapshot}",
                    $"{mat.Quantity:G}",
                    $"{mat.UnitPrice:N2} ج.م",
                    $"{mat.TotalPrice:N2} ج.م"
                ));
                mIdx++;
            }
            matTable.AppendChild(CreateCascadeWordRow("إجمالي الخامات الأولية", "", "", $"{m.RawMaterials:N2} ج.م", isBold: true));
            body.Append(matTable);

            // HIGH-18: Detailed Machinery Operations Table
            var pMacTitle = new Paragraph(
                new ParagraphProperties(new BiDi(), new SpacingBetweenLines() { Before = "360", After = "120" }),
                new Run(new RunProperties(new Bold(), new FontSize() { Val = "24" }, new RightToLeftText()),
                    new Text(SanitizeXml("ثانياً: بيان تشغيل الآلات والمراحل الفنية")) { Space = SpaceProcessingModeValues.Preserve })
            );
            body.Append(pMacTitle);

            var macTable = new Table();
            macTable.AppendChild(new TableProperties(
                new TableBorders(
                    new TopBorder() { Val = BorderValues.Single, Size = 6 },
                    new BottomBorder() { Val = BorderValues.Single, Size = 6 },
                    new InsideHorizontalBorder() { Val = BorderValues.Single, Size = 4 },
                    new InsideVerticalBorder() { Val = BorderValues.Single, Size = 4 }
                ),
                new TableWidth() { Type = TableWidthUnitValues.Pct, Width = "5000" },
                new TableJustification() { Val = TableRowAlignmentValues.Center },
                new BiDiVisual()
            ));

            macTable.AppendChild(CreateCascadeWordRow("م", "إسم الآلة / المرحلة", "ساعات العمل", "إجمالي التكلفة", isHeader: true));
            int mcIdx = 1;
            foreach (var mac in m.MachineryItems ?? Enumerable.Empty<MoqaysaMachineryItem>())
            {
                macTable.AppendChild(CreateCascadeWordRow(
                    $"{mcIdx}. {mac.MachineryNameSnapshot}",
                    $"عمال: {mac.Hours:G} س | ماكينة: {mac.Hours:G} س",
                    $"أجور: {mac.TotalWorkerWages:N2}",
                    $"تشغيل: {mac.TotalMachineOperatingCost:N2} ج.م"
                ));
                mcIdx++;
            }
            macTable.AppendChild(CreateCascadeWordRow("إجمالي أجور العمال ومصاريف الآلات", "", "", $"{(m.TotalWorkerWages + m.TotalMachineCosts):N2} ج.م", isBold: true));
            body.Append(macTable);

            // Signatures
            var pSig = new Paragraph(
                new ParagraphProperties(new BiDi(), new SpacingBetweenLines() { Before = "720" }),
                new Run(
                    new RunProperties(new Bold(), new FontSize() { Val = "22" }, new RightToLeftText()),
                    new Text(SanitizeXml("المختص: .....................          رئيس وحدة التكاليف: .....................          مدير عام إدارة المطبعة: .....................")) { Space = SpaceProcessingModeValues.Preserve }
                )
            );
            body.Append(pSig);

            body.Append(secPr);
            mainPart.Document.Save();
        }
        return mem.ToArray();
    }

    public byte[] ExportTasweyaToWord(Tasweya t)
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
            var bidi = new BiDi();
            secPr.Append(pgSz, pgMar, bidi);

            // Header with Safe Fallback
            AppendDocumentHeaderWithFallback(body, "كشف تسوية (استمارة ٥٥ ع.ح)");

            var pSub = new Paragraph(
                new ParagraphProperties(new BiDi()),
                new Run(
                    new RunProperties(new Bold(), new FontSize() { Val = "22" }, new RightToLeftText()),
                    new Text(SanitizeXml($"رقم التسوية: {t.SettlementNumber}          بتاريخ: {t.SettlementDate:yyyy/MM/dd}          مطالبة رقم: {t.ClaimNumber}")) { Space = SpaceProcessingModeValues.Preserve }
                )
            );
            body.Append(pSub);

            var pAccount = new Paragraph(
                new ParagraphProperties(new BiDi()),
                new Run(
                    new RunProperties(new Bold(), new FontSize() { Val = "24" }, new RightToLeftText()),
                    new Text(SanitizeXml($"{t.Account?.DebitHeaderTextAr ?? "من حـ / الإدارة العامة"}\n")) { Space = SpaceProcessingModeValues.Preserve }
                ),
                new Run(
                    new RunProperties(new FontSize() { Val = "22" }, new RightToLeftText()),
                    new Text(SanitizeXml($"إذن تشغيل رقم: {t.JobOrderNumbersText} | الجهة: {t.Customer?.CustomerNameAr ?? "غير محدد"}")) { Space = SpaceProcessingModeValues.Preserve }
                )
            );
            body.Append(pAccount);

            // Accounting breakdown table
            var table = new Table();
            var tblPr = new TableProperties(
                new TableBorders(
                    new TopBorder() { Val = BorderValues.Single, Size = 12 },
                    new BottomBorder() { Val = BorderValues.Single, Size = 12 },
                    new LeftBorder() { Val = BorderValues.Single, Size = 12 },
                    new RightBorder() { Val = BorderValues.Single, Size = 12 },
                    new InsideHorizontalBorder() { Val = BorderValues.Single, Size = 6 },
                    new InsideVerticalBorder() { Val = BorderValues.Single, Size = 6 }
                ),
                new TableWidth() { Type = TableWidthUnitValues.Pct, Width = "5000" },
                new TableJustification() { Val = TableRowAlignmentValues.Center },
                new BiDiVisual()
            );
            table.AppendChild(tblPr);

            table.AppendChild(CreateTasweyaWordRow("اجمالى", "أفرادى", "البيــــــــــــــــــــان", "اجمالى", "أفرادى", isHeader: true));
            table.AppendChild(CreateTasweyaWordRow("-", $"{t.SettlementGrandTotal:F2}", t.Account?.DebitHeaderTextAr ?? "من حـ / الإدارة العامة", "-", "-", isBold: true));
            table.AppendChild(CreateTasweyaWordRow("", "", $"إذن تشغيل رقم : {t.JobOrderNumbersText}", "", ""));
            table.AppendChild(CreateTasweyaWordRow("", "", $"الجهة : {t.Customer?.CustomerNameAr}", "", ""));
            table.AppendChild(CreateTasweyaWordRow("", "", $"مطالبة رقم : {t.ClaimNumber}", "", ""));
            table.AppendChild(CreateTasweyaWordRow("", "", "إلى مذكورين", "", "", isBold: true));
            table.AppendChild(CreateTasweyaWordRow("", "", "حـ / الإيرادات", $"{t.RevenuesTotal:F2}", "-", isBold: true));
            if (t.VatTotal > 0)
            {
                table.AppendChild(CreateTasweyaWordRow("", "", "حـ / القيمة المضافة", $"{t.VatTotal:F2}", "-", isBold: true));
            }
            table.AppendChild(CreateTasweyaWordRow("", "", "    خامـــــــات", "", $"{t.TotalMaterialsSum:F2}"));
            table.AppendChild(CreateTasweyaWordRow("", "", "    أجور عمال", "", $"{t.TotalWorkerWagesSum:F2}"));
            table.AppendChild(CreateTasweyaWordRow("", "", "    م . الآلات", "", $"{t.TotalMachineCostsSum:F2}"));
            if (t.AdminExpensesSum > 0) table.AppendChild(CreateTasweyaWordRow("", "", "    م . إدارية", "", $"{t.AdminExpensesSum:F2}"));
            if (t.MarkupAmountSum > 0) table.AppendChild(CreateTasweyaWordRow("", "", "    النسبة المضافة", "", $"{t.MarkupAmountSum:F2}"));
            if (t.MinistryOfFinanceSum > 0) table.AppendChild(CreateTasweyaWordRow("", "", "    وزارة المالية", "", $"{t.MinistryOfFinanceSum:F2}"));
            if (t.WorkersFundSum > 0) table.AppendChild(CreateTasweyaWordRow("", "", "    ص. ت. العاملين", "", $"{t.WorkersFundSum:F2}"));
            if (t.OtherAmountSum > 0) table.AppendChild(CreateTasweyaWordRow("", "", "    أخرى", "", $"{t.OtherAmountSum:F2}"));
            table.AppendChild(CreateTasweyaWordRow($"{t.SettlementGrandTotal:F2}", "-", $"المجموع المتوازن (فقط وقدره: {t.TotalAmountInWordsAr})", $"{t.SettlementGrandTotal:F2}", "-", isBold: true));

            body.Append(table);

            // Tafqeet
            var pTafqeet = new Paragraph(
                new ParagraphProperties(new BiDi(), new SpacingBetweenLines() { Before = "360" }),
                new Run(
                    new RunProperties(new Bold(), new FontSize() { Val = "24" }, new RightToLeftText()),
                    new Text(SanitizeXml($"فقط وقدره: {t.TotalAmountInWordsAr}")) { Space = SpaceProcessingModeValues.Preserve }
                )
            );
            body.Append(pTafqeet);

            // Signatures
            var pSig = new Paragraph(
                new ParagraphProperties(new BiDi(), new SpacingBetweenLines() { Before = "720" }),
                new Run(
                    new RunProperties(new Bold(), new FontSize() { Val = "22" }, new RightToLeftText()),
                    new Text(SanitizeXml("المراجع: .....................          رئيس الشئون المالية والإدارية: .....................          المراقب المالي: .....................")) { Space = SpaceProcessingModeValues.Preserve }
                )
            );
            body.Append(pSig);

            body.Append(secPr);
            mainPart.Document.Save();
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
            var bidi = new BiDi();
            secPr.Append(pgSz, pgMar, bidi);

            // Header with Safe Fallback
            AppendDocumentHeaderWithFallback(body, "أمر دفع واعتماد سداد رسمي");

            var pMeta = new Paragraph(
                new ParagraphProperties(new BiDi()),
                new Run(
                    new RunProperties(new Bold(), new FontSize() { Val = "24" }, new RightToLeftText()),
                    new Text(SanitizeXml($"رقم أمر الدفع: {p.PaymentOrderNumber} | التاريخ: {p.PaymentDate:yyyy/MM/dd}\n")) { Space = SpaceProcessingModeValues.Preserve }
                ),
                new Run(
                    new RunProperties(new FontSize() { Val = "22" }, new RightToLeftText()),
                    new Text(SanitizeXml($"الحساب: {p.Account?.AccountNameAr} | حالة السداد: {(p.IsConfirmed ? "مؤكد ومسدد رسمياً" : "مسودة غير مسددة")}")) { Space = SpaceProcessingModeValues.Preserve }
                )
            );
            body.Append(pMeta);

            var table = new Table();
            var tblPr = new TableProperties(
                new TableBorders(
                    new TopBorder() { Val = BorderValues.Single, Size = 12 },
                    new BottomBorder() { Val = BorderValues.Single, Size = 12 },
                    new LeftBorder() { Val = BorderValues.Single, Size = 12 },
                    new RightBorder() { Val = BorderValues.Single, Size = 12 },
                    new InsideHorizontalBorder() { Val = BorderValues.Single, Size = 6 },
                    new InsideVerticalBorder() { Val = BorderValues.Single, Size = 6 }
                ),
                new TableWidth() { Type = TableWidthUnitValues.Pct, Width = "5000" },
                new TableJustification() { Val = TableRowAlignmentValues.Center },
                new BiDiVisual()
            );
            table.AppendChild(tblPr);

            table.AppendChild(CreateTableRow("كشف التسوية / رقم المطالبة", "المبلغ المسدد (جنيه)", true));
            foreach (var link in p.TasweyaLinks)
            {
                table.AppendChild(CreateTableRow($"تسوية رقم {link.Tasweya?.SettlementNumber} (مطالبة {link.Tasweya?.ClaimNumber} - {link.Tasweya?.Customer?.CustomerNameAr})", $"{link.SnapshotTasweyaAmount:F2}"));
            }
            table.AppendChild(CreateTableRow("إجمالي المبلغ المسدد بأمر الدفع", $"{p.TotalPaidAmount:F2}", isBold: true));
            body.Append(table);

            var pTafqeet = new Paragraph(
                new ParagraphProperties(new BiDi(), new SpacingBetweenLines() { Before = "360" }),
                new Run(
                    new RunProperties(new Bold(), new FontSize() { Val = "24" }, new RightToLeftText()),
                    new Text(SanitizeXml($"المبلغ بالحروف: {p.TotalAmountInWordsAr}")) { Space = SpaceProcessingModeValues.Preserve }
                )
            );
            body.Append(pTafqeet);

            var pSig = new Paragraph(
                new ParagraphProperties(new BiDi(), new SpacingBetweenLines() { Before = "720" }),
                new Run(
                    new RunProperties(new Bold(), new FontSize() { Val = "22" }, new RightToLeftText()),
                    new Text(SanitizeXml("المحاسب المسؤول: .....................          مدير الحسابات: .....................          اعتماد المدير المالي: .....................")) { Space = SpaceProcessingModeValues.Preserve }
                )
            );
            body.Append(pSig);

            body.Append(secPr);
            mainPart.Document.Save();
        }
        return mem.ToArray();
    }

    private static TableRow CreateTableRow(string label, string val, bool isHeader = false, bool isBold = false)
    {
        var row = new TableRow();
        var c1 = new TableCell(
            new TableCellProperties(new TableCellWidth() { Type = TableWidthUnitValues.Pct, Width = "3500" }),
            new Paragraph(
                new ParagraphProperties(new BiDi()),
                new Run(
                    new RunProperties(
                        isHeader || isBold ? new Bold() : new RunProperties(),
                        new FontSize() { Val = isHeader ? "24" : "20" },
                        new RightToLeftText()
                    ),
                    new Text(SanitizeXml(label)) { Space = SpaceProcessingModeValues.Preserve }
                )
            )
        );

        var c2 = new TableCell(
            new TableCellProperties(new TableCellWidth() { Type = TableWidthUnitValues.Pct, Width = "1500" }),
            new Paragraph(
                new ParagraphProperties(new Justification() { Val = JustificationValues.Center }, new BiDi()),
                new Run(
                    new RunProperties(
                        isHeader || isBold ? new Bold() : new RunProperties(),
                        new FontSize() { Val = isHeader ? "24" : "20" },
                        new RightToLeftText()
                    ),
                    new Text(SanitizeXml(val)) { Space = SpaceProcessingModeValues.Preserve }
                )
            )
        );

        row.Append(c1, c2);
        return row;
    }

    private static TableRow CreateCascadeWordRow(string label, decimal? partialAmount, decimal? totalAmount, string note = "", bool isBold = false)
    {
        string pStr = partialAmount.HasValue ? $"{partialAmount.Value:F2}" : "-";
        string tStr = totalAmount.HasValue ? $"{totalAmount.Value:F2}" : "-";
        return CreateCascadeWordRow(label, pStr, tStr, note, isHeader: false, isBold: isBold);
    }

    private static TableRow CreateCascadeWordRow(string label, string partialStr, string totalStr, string note = "", bool isHeader = false, bool isBold = false)
    {
        var row = new TableRow();
        // In Word BiDi table, first cell is on the RIGHT (Elements side), and last cell is on the LEFT (Materials side)
        row.Append(
            CreateWordCell(label, "1800", isHeader, isBold, JustificationValues.Right),
            CreateWordCell(partialStr, "1100", isHeader, isBold, JustificationValues.Center),
            CreateWordCell(totalStr, "1100", isHeader, isBold, JustificationValues.Center),
            CreateWordCell(note, "1000", isHeader, isBold, JustificationValues.Right)
        );
        return row;
    }

    private static TableCell CreateWordCell(string text, string widthPct, bool isHeader, bool isBold, JustificationValues align)
    {
        return new TableCell(
            new TableCellProperties(new TableCellWidth() { Type = TableWidthUnitValues.Pct, Width = widthPct }),
            new Paragraph(
                new ParagraphProperties(new Justification() { Val = align }, new BiDi()),
                new Run(
                    new RunProperties(
                        isHeader || isBold ? new Bold() : new RunProperties(),
                        new FontSize() { Val = isHeader ? "22" : "18" },
                        new RightToLeftText()
                    ),
                    new Text(SanitizeXml(text)) { Space = SpaceProcessingModeValues.Preserve }
                )
            )
        );
    }

    private static TableRow CreateTasweyaWordRow(string rTotal, string rPartial, string statement, string lTotal, string lPartial, bool isHeader = false, bool isBold = false)
    {
        var row = new TableRow();
        row.Append(
            CreateWordCell(rTotal, "750", isHeader, isBold, JustificationValues.Center),   // 15% (Right Total)
            CreateWordCell(rPartial, "750", isHeader, isBold, JustificationValues.Center), // 15% (Right Partial)
            CreateWordCell(statement, "2000", isHeader, isBold, isHeader ? JustificationValues.Center : JustificationValues.Right), // 40% (Statement)
            CreateWordCell(lTotal, "750", isHeader, isBold, JustificationValues.Center),   // 15% (Left Total)
            CreateWordCell(lPartial, "750", isHeader, isBold, JustificationValues.Center)  // 15% (Left Partial)
        ); // 750 + 750 + 2000 + 750 + 750 = 5000 (100% of TableWidth)
        return row;
    }
}
