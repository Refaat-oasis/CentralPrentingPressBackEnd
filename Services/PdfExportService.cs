using System;
using System.Collections.Generic;
using System.Linq;
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
                page.Margin(0.6f, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.ContentFromRightToLeft();
                page.DefaultTextStyle(x => x.FontFamily("Arial").FontSize(8).FontColor(Colors.Black));

                page.Header().Column(col =>
                {
                    col.Item().Row(row =>
                    {
                        // Right: University Title & Customer (matches official reference photo)
                        row.RelativeItem(2).AlignRight().Column(c =>
                        {
                            c.Item().Text("جامعة أسيوط").Bold().FontSize(10);
                            c.Item().Text("مطبعة الجامعة المركزية").Bold().FontSize(11);
                            c.Item().Text($"الجهة الطالبة: {m.Customer?.CustomerNameAr ?? "غير محدد"}").FontSize(8.5f);
                            if (!string.IsNullOrWhiteSpace(m.SpecificationsText))
                            {
                                c.Item().Text($"المواصفات: {m.SpecificationsText}").FontSize(7.5f);
                            }
                        });

                        // Center: Job Order Number and Quantity
                        row.RelativeItem(3).AlignCenter().Column(c =>
                        {
                            c.Item().Text($"مقايسة لإذن تشغيل رقم ( {m.JobOrderNumber} )").Bold().FontSize(12).Underline();
                            c.Item().PaddingTop(2).Text($"الكمية المطلوبة: {m.RequiredQuantityText}").Bold().FontSize(8.5f);
                        });

                        // Left: Press Seal / ISO Box
                        row.RelativeItem(2).AlignLeft().Column(c =>
                        {
                            c.Item().Border(1).BorderColor(Colors.Black).Padding(3).Column(box =>
                            {
                                box.Item().AlignCenter().Text("A.U.P - مطبعة جامعة أسيوط").Bold().FontSize(8);
                                box.Item().AlignCenter().Text("نظام الجودة والاعتماد").FontSize(7);
                                box.Item().AlignCenter().Text("AU-GEA-CN-FRM-001").FontSize(6.5f);
                            });
                        });
                    });

                    col.Item().PaddingVertical(4).LineHorizontal(0.5f);
                });

                page.Content().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        // Right side: Elements & Calculation Cascade (7 columns = 8 sub-columns)
                        columns.RelativeColumn(3.6f); // Elements Name
                        columns.RelativeColumn(1.0f); // Hours
                        columns.RelativeColumn(0.9f); // Hourly Piaster
                        columns.RelativeColumn(1.3f); // Hourly Pound
                        columns.RelativeColumn(0.9f); // Partial Piaster
                        columns.RelativeColumn(1.4f); // Partial Pound
                        columns.RelativeColumn(0.9f); // Total Piaster
                        columns.RelativeColumn(1.4f); // Total Pound


                        // Left side: Materials (6 columns = 6 sub-columns)
                        columns.RelativeColumn(3.6f); // Material Name
                        columns.RelativeColumn(1.1f); // Quantity
                        columns.RelativeColumn(0.9f); // Unit Price Piaster
                        columns.RelativeColumn(1.3f); // Unit Price Pound
                        columns.RelativeColumn(0.9f); // Total Piaster
                        columns.RelativeColumn(1.4f); // Total Pound

                    });

                    // Master Header 1
                    table.Header(header =>
                    {
                        // Right Half: Elements & Machinery (8 cols)
                        header.Cell().RowSpan(3).Border(0.5f).Background(Colors.Grey.Lighten4).Padding(2).AlignCenter().AlignMiddle().Text("عناصر المقايسة").Bold().FontSize(8);
                        header.Cell().RowSpan(3).Border(0.5f).Background(Colors.Grey.Lighten4).Padding(2).AlignCenter().AlignMiddle().Text("الوقت\nبالساعة").Bold().FontSize(7);
                        header.Cell().ColumnSpan(2).RowSpan(2).Border(0.5f).Background(Colors.Grey.Lighten4).Padding(2).AlignCenter().AlignMiddle().Text("قيمة الساعة").Bold().FontSize(7.5f);
                        header.Cell().ColumnSpan(4).Border(0.5f).Background(Colors.Grey.Lighten4).Padding(2).AlignCenter().AlignMiddle().Text("القيمة").Bold().FontSize(8);

                        // Left Half: Materials (6 cols)
                        header.Cell().ColumnSpan(6).Border(0.5f).Background(Colors.Grey.Lighten4).Padding(2).AlignCenter().AlignMiddle().Text("الخامــــــــــــات").Bold().FontSize(8);

                        // Header Row 2
                        header.Cell().ColumnSpan(2).Border(0.5f).Background(Colors.Grey.Lighten4).Padding(2).AlignCenter().AlignMiddle().Text("جزئي").Bold().FontSize(7.5f);
                        header.Cell().ColumnSpan(2).Border(0.5f).Background(Colors.Grey.Lighten4).Padding(2).AlignCenter().AlignMiddle().Text("كلي").Bold().FontSize(7.5f);

                        header.Cell().RowSpan(2).Border(0.5f).Background(Colors.Grey.Lighten4).Padding(2).AlignCenter().AlignMiddle().Text("إسم الصنف").Bold().FontSize(7.5f);
                        header.Cell().RowSpan(2).Border(0.5f).Background(Colors.Grey.Lighten4).Padding(2).AlignCenter().AlignMiddle().Text("الكمية").Bold().FontSize(7.5f);
                        header.Cell().ColumnSpan(2).Border(0.5f).Background(Colors.Grey.Lighten4).Padding(2).AlignCenter().AlignMiddle().Text("ثمن الوحدة").Bold().FontSize(7.5f);
                        header.Cell().ColumnSpan(2).Border(0.5f).Background(Colors.Grey.Lighten4).Padding(2).AlignCenter().AlignMiddle().Text("الجملة").Bold().FontSize(7.5f);

                        // Header Row 3 (Pound / Piaster headers)
                        header.Cell().Border(0.5f).Background(Colors.Grey.Lighten5).Padding(1).AlignCenter().Text("ق").Bold().FontSize(7);
                        header.Cell().Border(0.5f).Background(Colors.Grey.Lighten5).Padding(1).AlignCenter().Text("جنيه").Bold().FontSize(7);
                        header.Cell().Border(0.5f).Background(Colors.Grey.Lighten5).Padding(1).AlignCenter().Text("ق").Bold().FontSize(7);
                        header.Cell().Border(0.5f).Background(Colors.Grey.Lighten5).Padding(1).AlignCenter().Text("جنيه").Bold().FontSize(7);
                        header.Cell().Border(0.5f).Background(Colors.Grey.Lighten5).Padding(1).AlignCenter().Text("ق").Bold().FontSize(7);
                        header.Cell().Border(0.5f).Background(Colors.Grey.Lighten5).Padding(1).AlignCenter().Text("جنيه").Bold().FontSize(7);

                        header.Cell().Border(0.5f).Background(Colors.Grey.Lighten5).Padding(1).AlignCenter().Text("ق").Bold().FontSize(7);
                        header.Cell().Border(0.5f).Background(Colors.Grey.Lighten5).Padding(1).AlignCenter().Text("جنيه").Bold().FontSize(7);
                        header.Cell().Border(0.5f).Background(Colors.Grey.Lighten5).Padding(1).AlignCenter().Text("ق").Bold().FontSize(7);
                        header.Cell().Border(0.5f).Background(Colors.Grey.Lighten5).Padding(1).AlignCenter().Text("جنيه").Bold().FontSize(7);
                    });

                    var materials = (m.MaterialItems ?? new List<MoqaysaMaterialItem>()).ToList();
                    var machineries = (m.MachineryItems ?? new List<MoqaysaMachineryItem>()).ToList();

                    // Helper to render a material cell block on left (4 sub-columns: Name, Qty, UnitPrice, Total)
                    void RenderMaterialCells(MoqaysaMaterialItem? mat)
                    {
                        table.Cell().Border(0.5f).Padding(1).AlignRight().Text(mat?.MaterialNameSnapshot ?? "").FontSize(7);
                        table.Cell().Border(0.5f).Padding(1).AlignCenter().Text(mat != null && mat.Quantity > 0 ? mat.Quantity.ToString("G") : "").FontSize(7);
                        RenderPoundPiaster(table, mat?.UnitPrice ?? 0, showDashIfZero: true);
                        RenderPoundPiaster(table, mat?.TotalPrice ?? 0, showDashIfZero: true);
                    }

                    // Row 0: أجور عمال Category Header
                    table.Cell().Border(0.5f).Background(Colors.Grey.Lighten5).Padding(1).AlignRight().Text("أجور عمال :").Bold();
                    table.Cell().Border(0.5f).Padding(1).Text("");
                    RenderEmptyCells(table, 6);

                    var mat0 = 0 < materials.Count ? materials[0] : null;
                    RenderMaterialCells(mat0);

                    // Worker Wages Rows (11 rows)
                    for (int i = 0; i < 11; i++)
                    {
                        var mac = i < machineries.Count ? machineries[i] : null;
                        var mat = (i + 1) < materials.Count ? materials[i + 1] : null;

                        // Right: Worker wages
                        table.Cell().Border(0.5f).Padding(1).AlignRight().Text(mac?.MachineryNameSnapshot ?? "").FontSize(7);
                        table.Cell().Border(0.5f).Padding(1).AlignCenter().Text(mac != null && mac.Hours > 0 ? mac.Hours.ToString("G") : "").FontSize(7);
                        RenderPoundPiaster(table, mac?.WorkerHourlyRateSnapshot ?? 0, showDashIfZero: true);
                        RenderPoundPiaster(table, mac?.TotalWorkerWages ?? 0, showDashIfZero: true);
                        table.Cell().Border(0.5f).Padding(1).AlignCenter().Text("-").FontSize(7);
                        table.Cell().Border(0.5f).Padding(1).AlignCenter().Text("-").FontSize(7);

                        // Left: Material
                        RenderMaterialCells(mat);
                    }

                    // Subtotal Worker Wages Row
                    table.Cell().Border(0.5f).Background(Colors.Grey.Lighten4).Padding(1).AlignRight().Text("جملة أجور العمال").Bold();
                    table.Cell().Border(0.5f).Background(Colors.Grey.Lighten4).Padding(1).AlignCenter().Text(machineries.Sum(x => x.Hours).ToString("G")).Bold();
                    RenderEmptyCells(table, 4);
                    RenderPoundPiaster(table, m.TotalWorkerWages, isBold: true);

                    // Material 12
                    var mat12 = 12 < materials.Count ? materials[12] : null;
                    RenderMaterialCells(mat12);

                    // Machine Operating Subheader: مصاريف الآلات
                    table.Cell().Border(0.5f).Background(Colors.Grey.Lighten5).Padding(1).AlignRight().Text("مصاريف الآلات :").Bold();
                    table.Cell().Border(0.5f).Padding(1).Text("");
                    RenderEmptyCells(table, 6);

                    var mat13 = 13 < materials.Count ? materials[13] : null;
                    RenderMaterialCells(mat13);

                    // Machine Operating Rows (11 rows)
                    for (int i = 0; i < 11; i++)
                    {
                        var mac = i < machineries.Count ? machineries[i] : null;
                        var mat = (i + 14) < materials.Count ? materials[i + 14] : null;

                        // Right: Machine Operating
                        table.Cell().Border(0.5f).Padding(1).AlignRight().Text(mac?.MachineryNameSnapshot ?? "").FontSize(7);
                        table.Cell().Border(0.5f).Padding(1).AlignCenter().Text(mac != null && mac.Hours > 0 ? mac.Hours.ToString("G") : "").FontSize(7);
                        RenderPoundPiaster(table, mac?.MachineHourlyRateSnapshot ?? 0, showDashIfZero: true);
                        RenderPoundPiaster(table, mac?.TotalMachineOperatingCost ?? 0, showDashIfZero: true);
                        table.Cell().Border(0.5f).Padding(1).AlignCenter().Text("-").FontSize(7);
                        table.Cell().Border(0.5f).Padding(1).AlignCenter().Text("-").FontSize(7);

                        // Left: Material
                        if (i < 10)
                        {
                            RenderMaterialCells(mat);
                        }
                        else
                        {
                            // Blank cutout on material row 24
                            table.Cell().ColumnSpan(6).Border(0.5f).Text("");
                        }
                    }

                    // Subtotal Machine Costs Row
                    // Right: جملة م . الآلات
                    table.Cell().Border(0.5f).Background(Colors.Grey.Lighten4).Padding(1).AlignRight().Text("جملة م . الآلات").Bold();
                    table.Cell().Border(0.5f).Background(Colors.Grey.Lighten4).Padding(1).AlignCenter().Text(machineries.Sum(x => x.Hours).ToString("G")).Bold();
                    RenderEmptyCells(table, 4);
                    RenderPoundPiaster(table, m.TotalMachineCosts, isBold: true);

                    // Left: Materials Box Row 1: الجملة :
                    table.Cell().ColumnSpan(2).Border(0.5f).Background(Colors.Grey.Lighten5).Padding(1).AlignRight().Text("الجملة :").Bold();
                    RenderEmptyCells(table, 2);
                    RenderPoundPiaster(table, m.RawMaterials, isBold: true);

                    // Financial Cascade 1: الخامات
                    // Right: الخامات
                    table.Cell().Border(0.5f).Padding(1).AlignRight().Text("الخامات :").Bold();
                    table.Cell().Border(0.5f).Padding(1).Text("");
                    RenderEmptyCells(table, 2);
                    RenderPoundPiaster(table, m.RawMaterials);
                    RenderEmptyCells(table, 2);

                    // Left: Materials Box Row 2: المرتجع :
                    table.Cell().ColumnSpan(2).Border(0.5f).Background(Colors.Grey.Lighten5).Padding(1).AlignRight().Text("المرتجع :").Bold();
                    RenderEmptyCells(table, 2);
                    RenderPoundPiaster(table, 0, showDashIfZero: true);

                    // Financial Cascade 2: م. تشغيل الخامات
                    // Right: م. تشغيل الخامات
                    table.Cell().Border(0.5f).Padding(1).AlignRight().Text($"م. تشغيل الخامات ({m.SelectedMaterialProcessingPercent}%)").Bold();
                    table.Cell().Border(0.5f).Padding(1).Text("");
                    RenderEmptyCells(table, 2);
                    RenderPoundPiaster(table, m.MaterialProcessingFee);
                    RenderEmptyCells(table, 2);

                    // Left: Materials Box Row 3: صافي الجملة :
                    table.Cell().ColumnSpan(2).Border(0.5f).Background(Colors.Grey.Lighten4).Padding(1).AlignRight().Text("صافي الجملة :").Bold();
                    RenderEmptyCells(table, 2);
                    RenderPoundPiaster(table, m.RawMaterials, isBold: true);

                    // Financial Cascade 3: م. تشغيل الأفلام
                    // Right: م. تشغيل الأفلام
                    table.Cell().Border(0.5f).Padding(1).AlignRight().Text("م. تشغيل الأفلام").Bold();
                    table.Cell().Border(0.5f).Padding(1).Text("");
                    RenderEmptyCells(table, 2);
                    RenderPoundPiaster(table, m.FilmsProcessingFee, showDashIfZero: true);
                    RenderEmptyCells(table, 2);

                    // Left: blank cutout covering all remaining cascade lines
                    table.Cell().ColumnSpan(6).RowSpan(12).Border(0.5f).Text("");

                    // Cascade 4: جملة الخامات
                    table.Cell().Border(0.5f).Background(Colors.Grey.Lighten5).Padding(1).AlignRight().Text("جملة الخامات").Bold();
                    RenderEmptyCells(table, 5);
                    RenderPoundPiaster(table, m.TotalMaterials, isBold: true);

                    // Cascade 5: الإجمالي (1)
                    table.Cell().Border(0.5f).Padding(1).AlignRight().Text("الإجمالي (1)").Bold();
                    RenderEmptyCells(table, 5);
                    RenderPoundPiaster(table, m.SubTotal1, isBold: true);

                    // Cascade 6: مصروفات إدارية
                    table.Cell().Border(0.5f).Padding(1).AlignRight().Text($"مصروفات إدارية {(m.ApplyAdminExpenses ? $"({m.SelectedAdminExpensesPercent}%)" : "")}").Bold();
                    RenderEmptyCells(table, 3);
                    RenderPoundPiaster(table, m.AdminExpenses, showDashIfZero: !m.ApplyAdminExpenses);
                    RenderEmptyCells(table, 2);

                    // Cascade 7: إجمالي المصروفات
                    table.Cell().Border(0.5f).Background(Colors.Grey.Lighten5).Padding(1).AlignRight().Text("إجمالي المصروفات").Bold();
                    RenderEmptyCells(table, 5);
                    RenderPoundPiaster(table, m.TotalExpenses, isBold: true);

                    // Cascade 8: النسبة
                    table.Cell().Border(0.5f).Padding(1).AlignRight().Text($"النسبة {(m.ApplyMarkup ? $"({m.SelectedMarkupPercent}%)" : "")}").Bold();
                    RenderEmptyCells(table, 3);
                    RenderPoundPiaster(table, m.MarkupAmount, showDashIfZero: !m.ApplyMarkup);
                    RenderEmptyCells(table, 2);

                    // Cascade 9: الجملة
                    table.Cell().Border(0.5f).Padding(1).AlignRight().Text("الجملة").Bold();
                    RenderEmptyCells(table, 5);
                    RenderPoundPiaster(table, m.TotalAfterMarkup, isBold: true);

                    // Cascade 10: وزارة المالية
                    table.Cell().Border(0.5f).Padding(1).AlignRight().Text($"وزارة المالية ({m.SelectedMinistryOfFinancePercent}%)").Bold();
                    RenderEmptyCells(table, 3);
                    RenderPoundPiaster(table, m.MinistryOfFinanceFee);
                    RenderEmptyCells(table, 2);

                    // Cascade 11: ص.ت.العاملين
                    table.Cell().Border(0.5f).Padding(1).AlignRight().Text($"ص.ت.العاملين ({m.SelectedWorkersFundPercent}%)").Bold();
                    RenderEmptyCells(table, 3);
                    RenderPoundPiaster(table, m.WorkersSyndicateFund);
                    RenderEmptyCells(table, 2);

                    // Cascade 12: الإجمالي (2)
                    table.Cell().Border(0.5f).Background(Colors.Grey.Lighten5).Padding(1).AlignRight().Text("الإجمالي (2)").Bold();
                    RenderEmptyCells(table, 5);
                    RenderPoundPiaster(table, m.SubTotal2, isBold: true);

                    // Cascade 13: أخرى
                    table.Cell().Border(0.5f).Padding(1).AlignRight().Text($"أخرى {(m.ApplyOther ? $"({m.SelectedOtherPercent}%)" : "")}").Bold();
                    RenderEmptyCells(table, 3);
                    RenderPoundPiaster(table, m.OtherAmount, showDashIfZero: !m.ApplyOther);
                    RenderEmptyCells(table, 2);

                    // Cascade 14: إجمالي (3)
                    table.Cell().Border(0.5f).Padding(1).AlignRight().Text("إجمالي (3)").Bold();
                    RenderEmptyCells(table, 5);
                    RenderPoundPiaster(table, m.SubTotal3, isBold: true);

                    // Cascade 15: القيمة الإجمالية النهائية
                    table.Cell().Border(0.5f).Background(Colors.Grey.Lighten3).Padding(2).AlignRight().Text("القيمة الإجمالية النهائية").Bold().FontSize(8.5f);
                    RenderEmptyCells(table, 5);
                    RenderPoundPiaster(table, m.FinalGrandTotal, isBold: true);
                });

                page.Footer().Column(col =>
                {
                    col.Item().PaddingTop(12).Row(row =>
                    {
                        row.RelativeItem().AlignCenter().Text("المختص\n....................").Bold().FontSize(8.5f);
                        row.RelativeItem().AlignCenter().Text("رئيس وحدة التكاليف\n....................").Bold().FontSize(8.5f);
                        row.RelativeItem().AlignCenter().Text("مدير عام إدارة المطبعة\n....................").Bold().FontSize(8.5f);
                    });
                });
            });
        });

        return document.GeneratePdf();
    }

    private static void RenderPoundPiaster(TableDescriptor table, decimal amount, bool isBold = false, bool showDashIfZero = false)
    {
        bool isZero = Math.Abs(amount) < 0.001m;
        if (showDashIfZero && isZero)
        {
            table.Cell().Border(0.5f).Padding(1).AlignCenter().Text("-").FontSize(7);
            table.Cell().Border(0.5f).Padding(1).AlignCenter().Text("-").FontSize(7);
            return;
        }

        long p = (long)Math.Floor(Math.Abs(amount));
        long pi = (long)Math.Round((Math.Abs(amount) - p) * 100);

        var cPi = table.Cell().Border(0.5f).Padding(1).AlignCenter().Text(pi.ToString("D2")).FontSize(7);
        if (isBold) cPi.Bold();

        var cP = table.Cell().Border(0.5f).Padding(1).AlignCenter().Text(p.ToString()).FontSize(7);
        if (isBold) cP.Bold();
    }

    private static void RenderEmptyCells(TableDescriptor table, int count)
    {
        for (int i = 0; i < count; i++)
        {
            table.Cell().Border(0.5f).Padding(1).Text("");
        }
    }

    private static void RenderEmptyDashes(TableDescriptor table, int count)
    {
        for (int i = 0; i < count; i++)
        {
            table.Cell().Border(0.5f).Padding(1).AlignCenter().Text("-");
        }
    }

    public byte[] ExportTasweyaToPdf(Tasweya t)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1.0f, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.ContentFromRightToLeft();
                page.DefaultTextStyle(x => x.FontFamily("Arial").FontSize(8).FontColor(Colors.Black));

                page.Header().Column(col =>
                {
                    col.Item().AlignCenter().Text("صندوق مطبعة جامعة أسيوط").Bold().FontSize(12);
                    col.Item().AlignCenter().Text("كشف تسوية").Bold().FontSize(16).Underline();
                    col.Item().PaddingTop(4).Row(row =>
                    {
                        row.RelativeItem().AlignRight().Text($"رقم التسوية: {t.SettlementNumber}").Bold();
                        row.RelativeItem().AlignCenter().Text($"بتاريخ: {t.SettlementDate:yyyy/MM/dd}").Bold();
                        row.RelativeItem().AlignLeft().Text($"مطالبة رقم: {t.ClaimNumber}").Bold();
                    });
                    col.Item().PaddingTop(2).LineHorizontal(0.5f);
                });

                page.Content().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        // Right: اجمالى (قرش, جنيه)
                        columns.RelativeColumn(0.9f);
                        columns.RelativeColumn(1.4f);
                        // Right: أفرادى (قرش, جنيه)
                        columns.RelativeColumn(0.9f);
                        columns.RelativeColumn(1.4f);

                        // Center: البيان
                        columns.RelativeColumn(5.0f);

                        // Left: اجمالى (قرش, جنيه)
                        columns.RelativeColumn(0.9f);
                        columns.RelativeColumn(1.4f);
                        // Left: أفرادى (قرش, جنيه)
                        columns.RelativeColumn(0.9f);
                        columns.RelativeColumn(1.4f);
                    });

                    // Master Header 1
                    table.Header(header =>
                    {
                        header.Cell().ColumnSpan(2).Border(0.5f).Background(Colors.Grey.Lighten4).Padding(2).AlignCenter().Text("اجمالى").Bold();
                        header.Cell().ColumnSpan(2).Border(0.5f).Background(Colors.Grey.Lighten4).Padding(2).AlignCenter().Text("أفرادى").Bold();
                        header.Cell().RowSpan(2).Border(0.5f).Background(Colors.Grey.Lighten4).Padding(2).AlignCenter().AlignMiddle().Text("البيــــــــــــــــــــان").Bold();
                        header.Cell().ColumnSpan(2).Border(0.5f).Background(Colors.Grey.Lighten4).Padding(2).AlignCenter().Text("اجمالى").Bold();
                        header.Cell().ColumnSpan(2).Border(0.5f).Background(Colors.Grey.Lighten4).Padding(2).AlignCenter().Text("أفرادى").Bold();

                        // Header Row 2
                        header.Cell().Border(0.5f).Background(Colors.Grey.Lighten5).Padding(1).AlignCenter().Text("قرش").FontSize(7);
                        header.Cell().Border(0.5f).Background(Colors.Grey.Lighten5).Padding(1).AlignCenter().Text("جنيه").FontSize(7);
                        header.Cell().Border(0.5f).Background(Colors.Grey.Lighten5).Padding(1).AlignCenter().Text("قرش").FontSize(7);
                        header.Cell().Border(0.5f).Background(Colors.Grey.Lighten5).Padding(1).AlignCenter().Text("جنيه").FontSize(7);

                        header.Cell().Border(0.5f).Background(Colors.Grey.Lighten5).Padding(1).AlignCenter().Text("قرش").FontSize(7);
                        header.Cell().Border(0.5f).Background(Colors.Grey.Lighten5).Padding(1).AlignCenter().Text("جنيه").FontSize(7);
                        header.Cell().Border(0.5f).Background(Colors.Grey.Lighten5).Padding(1).AlignCenter().Text("قرش").FontSize(7);
                        header.Cell().Border(0.5f).Background(Colors.Grey.Lighten5).Padding(1).AlignCenter().Text("جنيه").FontSize(7);
                    });

                    // Line 1: من حـ / {Account}
                    // Under اجمالى: - -
                    table.Cell().Border(0.5f).Padding(1).AlignCenter().Text("-");
                    table.Cell().Border(0.5f).Padding(1).AlignCenter().Text("-");
                    // Under أفرادى: Amount (Request 7 fix)
                    RenderPoundPiaster(table, t.SettlementGrandTotal, isBold: true);
                    // Center Statement
                    table.Cell().Border(0.5f).Padding(1).AlignRight().Text(t.Account?.DebitHeaderTextAr ?? "من حـ / الإدارة العامة").Bold().Underline();
                    // Left 4 cells
                    RenderEmptyDashes(table, 4);

                    // Line 2: إذن تشغيل رقم
                    RenderEmptyCells(table, 4);
                    table.Cell().Border(0.5f).Padding(1).AlignRight().Text($"إذن تشغيل رقم : {t.JobOrderNumbersText}");
                    RenderEmptyCells(table, 4);

                    // Line 3: الجهة
                    RenderEmptyCells(table, 4);
                    table.Cell().Border(0.5f).Padding(1).AlignRight().Text($"الجهة : {t.Customer?.CustomerNameAr}");
                    RenderEmptyCells(table, 4);

                    // Line 4: مطالبة رقم
                    RenderEmptyCells(table, 4);
                    table.Cell().Border(0.5f).Padding(1).AlignRight().Text($"مطالبة رقم : {t.ClaimNumber}");
                    RenderEmptyCells(table, 4);

                    // Line 5: إلى مذكورين
                    RenderEmptyCells(table, 4);
                    table.Cell().Border(0.5f).Padding(1).AlignCenter().Text("إلى مذكورين").Bold().Underline();
                    RenderEmptyCells(table, 4);

                    // Line 6: حـ / الإيرادات
                    RenderEmptyCells(table, 4);
                    table.Cell().Border(0.5f).Padding(1).AlignCenter().Text("حـ / الإيرادات").Bold().Underline();
                    RenderPoundPiaster(table, t.RevenuesTotal, isBold: true);
                    table.Cell().Border(0.5f).Padding(1).AlignCenter().Text("-");
                    table.Cell().Border(0.5f).Padding(1).AlignCenter().Text("-");

                    // Line 7: حـ / القيمة المضافة (if > 0)
                    if (t.VatTotal > 0)
                    {
                        RenderEmptyCells(table, 4);
                        table.Cell().Border(0.5f).Padding(1).AlignCenter().Text("حـ / القيمة المضافة").Bold().Underline();
                        RenderPoundPiaster(table, t.VatTotal, isBold: true);
                        table.Cell().Border(0.5f).Padding(1).AlignCenter().Text("-");
                        table.Cell().Border(0.5f).Padding(1).AlignCenter().Text("-");
                    }

                    void RenderTasweyaSubRow(string label, decimal amount)
                    {
                        RenderEmptyCells(table, 4);
                        table.Cell().Border(0.5f).Padding(1).AlignRight().Text($"    {label}");
                        RenderEmptyCells(table, 2);
                        RenderPoundPiaster(table, amount);
                    }

                    RenderTasweyaSubRow("خامـــــــات", t.TotalMaterialsSum);
                    RenderTasweyaSubRow("أجور عمال", t.TotalWorkerWagesSum);
                    RenderTasweyaSubRow("م . الآلات", t.TotalMachineCostsSum);
                    if (t.AdminExpensesSum > 0) RenderTasweyaSubRow("م . إدارية", t.AdminExpensesSum);
                    if (t.MarkupAmountSum > 0) RenderTasweyaSubRow("النسبة المضافة", t.MarkupAmountSum);
                    if (t.MinistryOfFinanceSum > 0) RenderTasweyaSubRow("وزارة المالية", t.MinistryOfFinanceSum);
                    if (t.WorkersFundSum > 0) RenderTasweyaSubRow("ص. ت. العاملين", t.WorkersFundSum);
                    if (t.OtherAmountSum > 0) RenderTasweyaSubRow("أخرى", t.OtherAmountSum);

                    // Master Balanced Total Row
                    RenderPoundPiaster(table, t.SettlementGrandTotal, isBold: true);
                    RenderEmptyCells(table, 2);
                    table.Cell().Border(1).Background(Colors.Grey.Lighten4).Padding(2).AlignCenter().Text($"فقط وقدره: {t.TotalAmountInWordsAr}").Bold().FontSize(7.5f);
                    RenderPoundPiaster(table, t.SettlementGrandTotal, isBold: true);
                    RenderEmptyCells(table, 2);
                });

                page.Footer().Column(col =>
                {
                    col.Item().PaddingTop(15).Row(row =>
                    {
                        row.RelativeItem().AlignCenter().Text("المراجع\n....................").Bold();
                        row.RelativeItem().AlignCenter().Text("رئيس الشئون المالية والإدارية\n....................").Bold();
                        row.RelativeItem().AlignCenter().Text("المراقب المالي\n....................").Bold();
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
                page.Margin(1.2f, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.ContentFromRightToLeft();
                page.DefaultTextStyle(x => x.FontFamily("Arial").FontSize(9).FontColor(Colors.Black));

                page.Header().Column(col =>
                {
                    col.Item().Row(row =>
                    {
                        // Right: Assiut Univ
                        row.RelativeItem(1).AlignCenter().Column(c =>
                        {
                            c.Item().AlignCenter().Text("جامعة أسيوط").Bold().FontSize(12);
                            c.Item().AlignCenter().Text("AU-GEA-CN-FRM-002").FontSize(7).FontColor(Colors.Grey.Darken1);
                        });

                        // Center: Title + Subtitle + Status Badge
                        row.RelativeItem(2.5f).AlignCenter().Column(c =>
                        {
                            c.Item().AlignCenter().Text("أمر دفع واعتماد سداد رسمي").Bold().FontSize(16).Underline();
                            c.Item().PaddingTop(2).AlignCenter().Text("مطبعة جامعة أسيوط المركزية").Bold().FontSize(11);
                            c.Item().PaddingTop(4).AlignCenter().Border(1).PaddingHorizontal(8).PaddingVertical(2).Text($"حالة السداد : {(p.IsConfirmed ? "مسدد ومعتمد رسمياً" : "مسودة غير مسددة")}").Bold().FontSize(8);
                        });

                        // Left: AUP Press Box
                        row.RelativeItem(1).AlignCenter().Column(c =>
                        {
                            c.Item().AlignCenter().Text("صندوق المطبعة").Bold().FontSize(12);
                            c.Item().AlignCenter().Text("A.U.P - المركزية").FontSize(7).FontColor(Colors.Grey.Darken1);
                        });
                    });

                    col.Item().PaddingTop(6).LineHorizontal(1.5f);
                });

                page.Content().PaddingTop(8).Column(col =>
                {
                    // Metadata Box
                    col.Item().Border(1).Background(Colors.Grey.Lighten5).Padding(6).Column(meta =>
                    {
                        meta.Item().Row(r =>
                        {
                            r.RelativeItem().Text(t =>
                            {
                                t.Span("رقم أمر الدفع : ").Bold();
                                t.Span(p.PaymentOrderNumber ?? "—").Bold().Underline();
                            });

                            r.RelativeItem().Text(t =>
                            {
                                t.Span("تاريخ أمر الدفع : ").Bold();
                                t.Span($"{p.PaymentDate:yyyy-MM-dd}");
                            });
                        });

                        meta.Item().PaddingTop(4).Row(r =>
                        {
                            r.RelativeItem().Text(t =>
                            {
                                t.Span("الحساب المدين : ").Bold();
                                t.Span(p.Account?.AccountNameAr ?? "—");
                            });

                            var custName = p.Customer?.CustomerNameAr ?? (p.TasweyaLinks.FirstOrDefault()?.Tasweya?.Customer?.CustomerNameAr ?? "كشوف تسوية متعددة");
                            r.RelativeItem().Text(t =>
                            {
                                t.Span("الجهة / العميل : ").Bold();
                                t.Span(custName);
                            });
                        });

                        if (!string.IsNullOrWhiteSpace(p.Notes))
                        {
                            meta.Item().PaddingTop(4).Text(t =>
                            {
                                t.Span("ملاحظات : ").Bold();
                                t.Span(p.Notes);
                            });
                        }
                    });

                    // Linked Settlements Table
                    col.Item().PaddingTop(10).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(0.7f); // م
                            columns.RelativeColumn(2.2f); // رقم التسوية
                            columns.RelativeColumn(2.2f); // رقم المطالبة
                            columns.RelativeColumn(4.5f); // الجهة الطالبة
                            columns.RelativeColumn(1.0f); // قرش
                            columns.RelativeColumn(1.6f); // جنيه
                        });

                        table.Header(header =>
                        {
                            header.Cell().RowSpan(2).Border(1).Background(Colors.Grey.Lighten4).Padding(3).AlignCenter().AlignMiddle().Text("م").Bold().FontSize(9);
                            header.Cell().RowSpan(2).Border(1).Background(Colors.Grey.Lighten4).Padding(3).AlignCenter().AlignMiddle().Text("رقم التسوية").Bold().FontSize(9);
                            header.Cell().RowSpan(2).Border(1).Background(Colors.Grey.Lighten4).Padding(3).AlignCenter().AlignMiddle().Text("رقم المطالبة").Bold().FontSize(9);
                            header.Cell().RowSpan(2).Border(1).Background(Colors.Grey.Lighten4).Padding(3).AlignCenter().AlignMiddle().Text("الجهة الطالبة").Bold().FontSize(9);
                            header.Cell().ColumnSpan(2).Border(1).Background(Colors.Grey.Lighten4).Padding(2).AlignCenter().Text("المبلغ المسدد").Bold().FontSize(9);

                            header.Cell().Border(1).Background(Colors.Grey.Lighten4).Padding(2).AlignCenter().Text("قرش").Bold().FontSize(8);
                            header.Cell().Border(1).Background(Colors.Grey.Lighten4).Padding(2).AlignCenter().Text("جنيه").Bold().FontSize(8);
                        });

                        int idx = 1;
                        foreach (var link in p.TasweyaLinks)
                        {
                            long pVal = (long)Math.Truncate(link.SnapshotTasweyaAmount);
                            int piVal = (int)Math.Round((link.SnapshotTasweyaAmount - pVal) * 100);
                            if (piVal < 0) piVal = 0;

                            table.Cell().Border(1).Padding(3).AlignCenter().Text(idx.ToString()).FontSize(9);
                            table.Cell().Border(1).Padding(3).AlignCenter().Text(link.Tasweya?.SettlementNumber ?? "—").Bold().FontSize(9);
                            table.Cell().Border(1).Padding(3).AlignCenter().Text(link.Tasweya?.ClaimNumber ?? "—").FontSize(9);
                            table.Cell().Border(1).Padding(3).AlignRight().PaddingRight(4).Text(link.Tasweya?.Customer?.CustomerNameAr ?? "—").FontSize(9);
                            table.Cell().Border(1).Padding(3).AlignCenter().Text(piVal.ToString("D2")).FontSize(9);
                            table.Cell().Border(1).Padding(3).AlignCenter().Text(pVal.ToString()).FontSize(9);

                            idx++;
                        }

                        // Total Row
                        long totP = (long)Math.Truncate(p.TotalPaidAmount);
                        int totPi = (int)Math.Round((p.TotalPaidAmount - totP) * 100);
                        if (totPi < 0) totPi = 0;

                        table.Cell().ColumnSpan(4).Border(1.5f).Background(Colors.Grey.Lighten3).Padding(4).AlignRight().PaddingRight(6).Text("إجمالي المبلغ المسدد بأمر الدفع :").Bold().FontSize(10);
                        table.Cell().Border(1.5f).Background(Colors.Grey.Lighten3).Padding(4).AlignCenter().Text(totPi.ToString("D2")).Bold().FontSize(10);
                        table.Cell().Border(1.5f).Background(Colors.Grey.Lighten3).Padding(4).AlignCenter().Text(totP.ToString()).Bold().FontSize(10);
                    });

                    // Tafqeet Box
                    col.Item().PaddingTop(12).Border(1.5f).Padding(6).AlignCenter().Text(p.TotalAmountInWordsAr ?? "").Bold().FontSize(11);
                });

                page.Footer().Column(col =>
                {
                    col.Item().PaddingTop(20).Row(row =>
                    {
                        row.RelativeItem().AlignCenter().Text("المحاسب المسؤول\n\n...................................").Bold();
                        row.RelativeItem().AlignCenter().Text("مدير إدارة الحسابات\n\n...................................").Bold();
                        row.RelativeItem().AlignCenter().Text("اعتماد المراقب المالي\n\n...................................").Bold();
                    });
                });
            });
        });

        return document.GeneratePdf();
    }
}
