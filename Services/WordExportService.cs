using System;
using System.IO;
using CentralPress.API.Entities;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace CentralPress.API.Services;

public class WordExportService
{
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

            // Title Paragraph
            var pTitle = new Paragraph(
                new ParagraphProperties(new Justification() { Val = JustificationValues.Center }, new BiDi()),
                new Run(
                    new RunProperties(new Bold(), new FontSize() { Val = "32" }, new RightToLeftText()),
                    new Text($"مقايسة لإذن تشغيل رقم ( {m.JobOrderNumber} )")
                )
            );
            body.Append(pTitle);

            // Customer & Specifications
            var pMeta = new Paragraph(
                new ParagraphProperties(new BiDi()),
                new Run(
                    new RunProperties(new Bold(), new FontSize() { Val = "24" }, new RightToLeftText()),
                    new Text($"الجهة الطالبة: {m.Customer?.CustomerNameAr ?? "غير محدد"} | الكمية المطلوبة: {m.RequiredQuantityText}")
                )
            );
            body.Append(pMeta);

            if (!string.IsNullOrWhiteSpace(m.SpecificationsText))
            {
                var pSpecs = new Paragraph(
                    new ParagraphProperties(new BiDi()),
                    new Run(
                        new RunProperties(new FontSize() { Val = "22" }, new RightToLeftText()),
                        new Text($"المواصفات: {m.SpecificationsText}")
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
                new TableJustification() { Val = TableRowAlignmentValues.Center }
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
            table.AppendChild(CreateCascadeWordRow($"م. تشغيل الخامات ({m.SelectedMaterialProcessingPercent}%)", m.MaterialProcessingFee, null));

            // Cascade 3: Films
            if (m.FilmsProcessingFee > 0)
                table.AppendChild(CreateCascadeWordRow("م. تشغيل الأفلام", m.FilmsProcessingFee, null));

            // Cascade 4: Total Materials
            table.AppendChild(CreateCascadeWordRow("جملة الخامات", null, m.TotalMaterials, isBold: true));

            // Cascade 5: Subtotal 1
            table.AppendChild(CreateCascadeWordRow("الإجمالي (1)", null, m.SubTotal1, isBold: true));

            // Cascade 6: Admin Expenses
            table.AppendChild(CreateCascadeWordRow($"مصروفات إدارية {(m.ApplyAdminExpenses ? $\"({m.SelectedAdminExpensesPercent}%)\" : \"\")}", m.ApplyAdminExpenses ? m.AdminExpenses : null, null));

            // Cascade 7: Total Expenses
            table.AppendChild(CreateCascadeWordRow("إجمالي المصروفات", null, m.TotalExpenses, isBold: true));

            // Cascade 8: Markup
            table.AppendChild(CreateCascadeWordRow($"النسبة {(m.ApplyMarkup ? $"({m.SelectedMarkupPercent}%)" : "")}", m.ApplyMarkup ? m.MarkupAmount : null, null));

            // Cascade 9: Total After Markup
            table.AppendChild(CreateCascadeWordRow("الجملة", null, m.TotalAfterMarkup, isBold: true));

            // Cascade 10: Ministry of Finance
            table.AppendChild(CreateCascadeWordRow($"وزارة المالية ({m.SelectedMinistryOfFinancePercent}%)", m.MinistryOfFinanceFee, null));

            // Cascade 11: Workers Fund
            table.AppendChild(CreateCascadeWordRow($"ص. ت. العاملين ({m.SelectedWorkersFundPercent}%)", m.WorkersSyndicateFund, null));

            // Cascade 12: Subtotal 2
            table.AppendChild(CreateCascadeWordRow("الإجمالي (2)", null, m.SubTotal2, isBold: true));

            // Cascade 13: Other
            if (m.ApplyOther)
                table.AppendChild(CreateCascadeWordRow($"أخرى {(m.ApplyOther ? $\"({m.SelectedOtherPercent}%)\" : \"\")}", m.OtherAmount, null));

            // Cascade 14: Subtotal 3
            table.AppendChild(CreateCascadeWordRow("إجمالي (3)", null, m.SubTotal3, isBold: true));

            // Cascade 15: VAT
            if (m.ApplyVat)
                table.AppendChild(CreateCascadeWordRow($"ض. القيمة المضافة {(m.ApplyVat ? $\"({m.SelectedVatPercent}%)\" : \"\")}", m.VatAmount, null));

            // Final Grand Total
            table.AppendChild(CreateCascadeWordRow("القيمة الإجمالية النهائية", null, m.FinalGrandTotal, isBold: true));


            body.Append(table);

            // Signatures
            var pSig = new Paragraph(
                new ParagraphProperties(new BiDi(), new SpacingBetweenLines() { Before = "720" }),
                new Run(
                    new RunProperties(new Bold(), new FontSize() { Val = "22" }, new RightToLeftText()),
                    new Text("المختص: .....................          رئيس وحدة التكاليف: .....................          مدير عام إدارة المطبعة: .....................")
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

            // Header
            var pHeader = new Paragraph(
                new ParagraphProperties(new Justification() { Val = JustificationValues.Center }, new BiDi()),
                new Run(
                    new RunProperties(new Bold(), new FontSize() { Val = "26" }, new RightToLeftText()),
                    new Text("صندوق مطبعة جامعة أسيوط\n")
                ),
                new Run(
                    new RunProperties(new Bold(), new FontSize() { Val = "32" }, new RightToLeftText()),
                    new Text("كشف تسوية\n")
                )
            );
            body.Append(pHeader);

            var pSub = new Paragraph(
                new ParagraphProperties(new BiDi()),
                new Run(
                    new RunProperties(new Bold(), new FontSize() { Val = "22" }, new RightToLeftText()),
                    new Text($"رقم التسوية: {t.SettlementNumber}          بتاريخ: {t.SettlementDate:yyyy/MM/dd}          مطالبة رقم: {t.ClaimNumber}")
                )
            );
            body.Append(pSub);

            var pAccount = new Paragraph(
                new ParagraphProperties(new BiDi()),
                new Run(
                    new RunProperties(new Bold(), new FontSize() { Val = "24" }, new RightToLeftText()),
                    new Text($"{t.Account?.DebitHeaderTextAr ?? "من حـ / الإدارة العامة"}\n")
                ),
                new Run(
                    new RunProperties(new FontSize() { Val = "22" }, new RightToLeftText()),
                    new Text($"إذن تشغيل رقم: {t.JobOrderNumbersText} | الجهة: {t.Customer?.CustomerNameAr ?? "غير محدد"}")
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
                new TableJustification() { Val = TableRowAlignmentValues.Center }
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
                    new Text($"فقط وقدره: {t.TotalAmountInWordsAr}")
                )
            );
            body.Append(pTafqeet);

            // Signatures
            var pSig = new Paragraph(
                new ParagraphProperties(new BiDi(), new SpacingBetweenLines() { Before = "720" }),
                new Run(
                    new RunProperties(new Bold(), new FontSize() { Val = "22" }, new RightToLeftText()),
                    new Text("المراجع: .....................          رئيس الشئون المالية والإدارية: .....................          المراقب المالي: .....................")
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

            var pHeader = new Paragraph(
                new ParagraphProperties(new Justification() { Val = JustificationValues.Center }, new BiDi()),
                new Run(
                    new RunProperties(new Bold(), new FontSize() { Val = "34" }, new RightToLeftText()),
                    new Text("مطبعة جامعة أسيوط - أمر دفع واعتماد سداد رسمي\n")
                )
            );
            body.Append(pHeader);

            var pMeta = new Paragraph(
                new ParagraphProperties(new BiDi()),
                new Run(
                    new RunProperties(new Bold(), new FontSize() { Val = "24" }, new RightToLeftText()),
                    new Text($"رقم أمر الدفع: {p.PaymentOrderNumber} | التاريخ: {p.PaymentDate:yyyy/MM/dd}\n")
                ),
                new Run(
                    new RunProperties(new FontSize() { Val = "22" }, new RightToLeftText()),
                    new Text($"الحساب: {p.Account?.AccountNameAr} | حالة السداد: {(p.IsConfirmed ? "مؤكد ومسدد رسمياً" : "مسودة غير مسددة")}")
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
                new TableJustification() { Val = TableRowAlignmentValues.Center }
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
                    new Text($"المبلغ بالحروف: {p.TotalAmountInWordsAr}")
                )
            );
            body.Append(pTafqeet);

            var pSig = new Paragraph(
                new ParagraphProperties(new BiDi(), new SpacingBetweenLines() { Before = "720" }),
                new Run(
                    new RunProperties(new Bold(), new FontSize() { Val = "22" }, new RightToLeftText()),
                    new Text("المحاسب المسؤول: .....................          مدير الحسابات: .....................          اعتماد المدير المالي: .....................")
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
                    new Text(label)
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
                    new Text(val)
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
                    new Text(text ?? "")
                )
            )
        );
    }

    private static TableRow CreateTasweyaWordRow(string rTotal, string rPartial, string statement, string lTotal, string lPartial, bool isHeader = false, bool isBold = false)
    {
        var row = new TableRow();
        row.Append(
            CreateWordCell(rTotal, "1000", isHeader, isBold, JustificationValues.Center),
            CreateWordCell(rPartial, "1000", isHeader, isBold, JustificationValues.Center),
            CreateWordCell(statement, "2000", isHeader, isBold, isHeader ? JustificationValues.Center : JustificationValues.Right),
            CreateWordCell(lTotal, "1000", isHeader, isBold, JustificationValues.Center),
            CreateWordCell(lPartial, "1000", isHeader, isBold, JustificationValues.Center)
        );
        return row;
    }
}
