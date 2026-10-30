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

            // Add Header Row
            table.AppendChild(CreateTableRow("البيــــــــان", "القيمة (جنيه)", true));
            table.AppendChild(CreateTableRow("الخامات", $"{m.RawMaterials:F2}"));
            table.AppendChild(CreateTableRow("م. تشغيل الخامات (5.5%)", $"{m.MaterialProcessingFee:F2}"));
            table.AppendChild(CreateTableRow("جملة الخامات", $"{m.TotalMaterials:F2}"));
            table.AppendChild(CreateTableRow("جملة أجور العمال", $"{m.TotalWorkerWages:F2}"));
            table.AppendChild(CreateTableRow("جملة م. الآلات", $"{m.TotalMachineCosts:F2}"));
            table.AppendChild(CreateTableRow("الإجمالي (1)", $"{m.SubTotal1:F2}"));
            if (m.ApplyAdminExpenses) table.AppendChild(CreateTableRow("مصروفات إدارية (8.5%)", $"{m.AdminExpenses:F2}"));
            table.AppendChild(CreateTableRow("إجمالي المصروفات", $"{m.TotalExpenses:F2}"));
            if (m.ApplyMarkup) table.AppendChild(CreateTableRow($"النسبة ({m.SelectedMarkupPercent}%)", $"{m.MarkupAmount:F2}"));
            table.AppendChild(CreateTableRow("الجملة", $"{m.TotalAfterMarkup:F2}"));
            table.AppendChild(CreateTableRow("وزارة المالية (15%)", $"{m.MinistryOfFinanceFee:F2}"));
            table.AppendChild(CreateTableRow("ص. ت. العاملين (10%)", $"{m.WorkersSyndicateFund:F2}"));
            table.AppendChild(CreateTableRow("الإجمالي (2)", $"{m.SubTotal2:F2}"));
            if (m.ApplyOther) table.AppendChild(CreateTableRow("أخرى (10%)", $"{m.OtherAmount:F2}"));
            table.AppendChild(CreateTableRow("إجمالي (3)", $"{m.SubTotal3:F2}"));
            if (m.ApplyVat) table.AppendChild(CreateTableRow("ض. القيمة المضافة (14%)", $"{m.VatAmount:F2}"));
            table.AppendChild(CreateTableRow("القيمة الإجمالية النهائية", $"{m.FinalGrandTotal:F2}", isBold: true));

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

            table.AppendChild(CreateTableRow("البيــــــــان", "المبلغ (جنيه)", true));
            table.AppendChild(CreateTableRow("خامـــــــات", $"{t.TotalMaterialsSum:F2}"));
            table.AppendChild(CreateTableRow("أجور عمال", $"{t.TotalWorkerWagesSum:F2}"));
            table.AppendChild(CreateTableRow("م . الآلات", $"{t.TotalMachineCostsSum:F2}"));
            table.AppendChild(CreateTableRow("م . إدارية", $"{t.AdminExpensesSum:F2}"));
            table.AppendChild(CreateTableRow("النسبة المضافة", $"{t.MarkupAmountSum:F2}"));
            table.AppendChild(CreateTableRow("وزارة المالية", $"{t.MinistryOfFinanceSum:F2}"));
            table.AppendChild(CreateTableRow("ص. ت. العاملين", $"{t.WorkersFundSum:F2}"));
            if (t.OtherAmountSum > 0) table.AppendChild(CreateTableRow("أخرى", $"{t.OtherAmountSum:F2}"));
            table.AppendChild(CreateTableRow("حـ / الإيرادات", $"{t.RevenuesTotal:F2}", isBold: true));
            if (t.VatTotal > 0) table.AppendChild(CreateTableRow("حـ / القيمة المضافة", $"{t.VatTotal:F2}", isBold: true));
            table.AppendChild(CreateTableRow("الإجمالي الكلي للتسوية (المطالبة)", $"{t.SettlementGrandTotal:F2}", isBold: true));

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
}
