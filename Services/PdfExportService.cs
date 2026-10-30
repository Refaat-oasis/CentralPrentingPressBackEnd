using System;
using CentralPress.API.Entities;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace CentralPress.API.Services;

public class PdfExportService
{
    static PdfExportService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public byte[] ExportMoqaysaToPdf(Moqaysa m)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1.5f, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontFamily("Arial").FontSize(10).FontColor(Colors.Black));

                page.Header().Column(col =>
                {
                    col.Item().AlignCenter().Text($"جامعة أسيوط - مطبعة الجامعة المركزية").Bold().FontSize(14);
                    col.Item().AlignCenter().Text($"مقايسة لإذن تشغيل رقم ( {m.JobOrderNumber} )").Bold().FontSize(16);
                    col.Item().PaddingTop(5).Row(row =>
                    {
                        row.RelativeItem().AlignRight().Text($"الجهة الطالبة: {m.Customer?.CustomerNameAr ?? "غير محدد"}");
                        row.RelativeItem().AlignLeft().Text($"الكمية المطلوبة: {m.RequiredQuantityText}");
                    });
                    if (!string.IsNullOrWhiteSpace(m.SpecificationsText))
                    {
                        col.Item().PaddingTop(2).Text($"المواصفات: {m.SpecificationsText}");
                    }
                    col.Item().PaddingVertical(5).LineHorizontal(1);
                });

                page.Content().Column(col =>
                {
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(3);
                            columns.RelativeColumn(1);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Border(1).Background(Colors.Grey.Lighten3).Padding(4).Text("عناصر الحساب والمقايسة").Bold();
                            header.Cell().Border(1).Background(Colors.Grey.Lighten3).Padding(4).AlignCenter().Text("القيمة (جنيه)").Bold();
                        });

                        void AddRow(string label, decimal amount, bool isBold = false)
                        {
                            var c1 = table.Cell().Border(1).Padding(3).Text(label);
                            if (isBold) c1.Bold();

                            var c2 = table.Cell().Border(1).Padding(3).AlignCenter().Text($"{amount:F2}");
                            if (isBold) c2.Bold();
                        }

                        AddRow("الخامات", m.RawMaterials);
                        AddRow("م. تشغيل الخامات (5.5%)", m.MaterialProcessingFee);
                        AddRow("جملة الخامات", m.TotalMaterials);
                        AddRow("جملة أجور العمال", m.TotalWorkerWages);
                        AddRow("جملة م. الآلات", m.TotalMachineCosts);
                        AddRow("الإجمالي (1)", m.SubTotal1);
                        if (m.ApplyAdminExpenses) AddRow("مصروفات إدارية (8.5%)", m.AdminExpenses);
                        AddRow("إجمالي المصروفات", m.TotalExpenses);
                        if (m.ApplyMarkup) AddRow($"النسبة ({m.SelectedMarkupPercent}%)", m.MarkupAmount);
                        AddRow("الجملة", m.TotalAfterMarkup);
                        AddRow("وزارة المالية (15%)", m.MinistryOfFinanceFee);
                        AddRow("ص. ت. العاملين (10%)", m.WorkersSyndicateFund);
                        AddRow("الإجمالي (2)", m.SubTotal2);
                        if (m.ApplyOther) AddRow("أخرى (10%)", m.OtherAmount);
                        AddRow("إجمالي (3)", m.SubTotal3);
                        if (m.ApplyVat) AddRow("ض. القيمة المضافة (14%)", m.VatAmount);
                        AddRow("القيمة الإجمالية النهائية", m.FinalGrandTotal, isBold: true);
                    });
                });

                page.Footer().Column(col =>
                {
                    col.Item().PaddingTop(20).Row(row =>
                    {
                        row.RelativeItem().AlignCenter().Text("المختص\n....................");
                        row.RelativeItem().AlignCenter().Text("رئيس وحدة التكاليف\n....................");
                        row.RelativeItem().AlignCenter().Text("مدير عام إدارة المطبعة\n....................");
                    });
                });
            });
        });

        return document.GeneratePdf();
    }

    public byte[] ExportTasweyaToPdf(Tasweya t)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1.5f, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontFamily("Arial").FontSize(10).FontColor(Colors.Black));

                page.Header().Column(col =>
                {
                    col.Item().AlignCenter().Text("صندوق مطبعة جامعة أسيوط").Bold().FontSize(14);
                    col.Item().AlignCenter().Text("كشف تسوية").Bold().FontSize(18).Underline();
                    col.Item().PaddingTop(6).Row(row =>
                    {
                        row.RelativeItem().AlignRight().Text($"رقم التسوية: {t.SettlementNumber}");
                        row.RelativeItem().AlignCenter().Text($"التاريخ: {t.SettlementDate:yyyy/MM/dd}");
                        row.RelativeItem().AlignLeft().Text($"مطالبة رقم: {t.ClaimNumber}");
                    });
                    col.Item().PaddingTop(4).Text($"{t.Account?.DebitHeaderTextAr ?? "من حـ / الإدارة العامة"}").Bold().FontSize(12);
                    col.Item().Text($"إذن تشغيل رقم: {t.JobOrderNumbersText} | الجهة: {t.Customer?.CustomerNameAr}");
                    col.Item().PaddingVertical(5).LineHorizontal(1);
                });

                page.Content().Column(col =>
                {
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(3);
                            columns.RelativeColumn(1);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Border(1).Background(Colors.Grey.Lighten3).Padding(4).Text("البيــــــــان").Bold();
                            header.Cell().Border(1).Background(Colors.Grey.Lighten3).Padding(4).AlignCenter().Text("المبلغ (جنيه)").Bold();
                        });

                        void AddRow(string label, decimal amount, bool isBold = false)
                        {
                            var c1 = table.Cell().Border(1).Padding(3).Text(label);
                            if (isBold) c1.Bold();

                            var c2 = table.Cell().Border(1).Padding(3).AlignCenter().Text($"{amount:F2}");
                            if (isBold) c2.Bold();
                        }

                        AddRow("خامـــــــات", t.TotalMaterialsSum);
                        AddRow("أجور عمال", t.TotalWorkerWagesSum);
                        AddRow("م . الآلات", t.TotalMachineCostsSum);
                        AddRow("م . إدارية", t.AdminExpensesSum);
                        AddRow("النسبة المضافة", t.MarkupAmountSum);
                        AddRow("وزارة المالية", t.MinistryOfFinanceSum);
                        AddRow("ص. ت. العاملين", t.WorkersFundSum);
                        if (t.OtherAmountSum > 0) AddRow("أخرى", t.OtherAmountSum);
                        AddRow("حـ / الإيرادات", t.RevenuesTotal, isBold: true);
                        if (t.VatTotal > 0) AddRow("حـ / القيمة المضافة", t.VatTotal, isBold: true);
                        AddRow("الإجمالي الكلي للتسوية (المطالبة)", t.SettlementGrandTotal, isBold: true);
                    });

                    col.Item().PaddingTop(10).Text($"فقط وقدره: {t.TotalAmountInWordsAr}").Bold().FontSize(11);
                });

                page.Footer().Column(col =>
                {
                    col.Item().PaddingTop(20).Row(row =>
                    {
                        row.RelativeItem().AlignCenter().Text("المراجع\n....................");
                        row.RelativeItem().AlignCenter().Text("رئيس الشئون المالية والإدارية\n....................");
                        row.RelativeItem().AlignCenter().Text("المراقب المالي\n....................");
                    });
                });
            });
        });

        return document.GeneratePdf();
    }

    public byte[] ExportPaymentOrderToPdf(PaymentOrder p)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1.5f, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontFamily("Arial").FontSize(10).FontColor(Colors.Black));

                page.Header().Column(col =>
                {
                    col.Item().AlignCenter().Text("مطبعة جامعة أسيوط - أمر دفع واعتماد سداد رسمي").Bold().FontSize(16);
                    col.Item().PaddingTop(8).Row(row =>
                    {
                        row.RelativeItem().AlignRight().Text($"رقم أمر الدفع: {p.PaymentOrderNumber}").Bold();
                        row.RelativeItem().AlignCenter().Text($"التاريخ: {p.PaymentDate:yyyy/MM/dd}");
                        row.RelativeItem().AlignLeft().Text($"الحالة: {(p.IsConfirmed ? "مؤكد ومسدد" : "مسودة")}");
                    });
                    col.Item().PaddingTop(4).Text($"الحساب: {p.Account?.AccountNameAr}");
                    col.Item().PaddingVertical(5).LineHorizontal(1);
                });

                page.Content().Column(col =>
                {
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(3);
                            columns.RelativeColumn(1);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Border(1).Background(Colors.Grey.Lighten3).Padding(4).Text("كشف التسوية / المطالبة").Bold();
                            header.Cell().Border(1).Background(Colors.Grey.Lighten3).Padding(4).AlignCenter().Text("المبلغ (جنيه)").Bold();
                        });

                        foreach (var link in p.TasweyaLinks)
                        {
                            table.Cell().Border(1).Padding(3).Text($"تسوية رقم {link.Tasweya?.SettlementNumber} (مطالبة {link.Tasweya?.ClaimNumber} - {link.Tasweya?.Customer?.CustomerNameAr})");
                            table.Cell().Border(1).Padding(3).AlignCenter().Text($"{link.SnapshotTasweyaAmount:F2}");
                        }

                        table.Cell().Border(1).Padding(4).Text("إجمالي المبلغ المسدد").Bold();
                        table.Cell().Border(1).Padding(4).AlignCenter().Text($"{p.TotalPaidAmount:F2}").Bold();
                    });

                    col.Item().PaddingTop(10).Text($"المبلغ بالحروف: {p.TotalAmountInWordsAr}").Bold().FontSize(11);
                });

                page.Footer().Column(col =>
                {
                    col.Item().PaddingTop(25).Row(row =>
                    {
                        row.RelativeItem().AlignCenter().Text("المحاسب المسؤول\n....................");
                        row.RelativeItem().AlignCenter().Text("مدير الحسابات\n....................");
                        row.RelativeItem().AlignCenter().Text("اعتماد المدير المالي\n....................");
                    });
                });
            });
        });

        return document.GeneratePdf();
    }
}
