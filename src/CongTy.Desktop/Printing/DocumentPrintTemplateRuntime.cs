using System.Diagnostics;
using System.Printing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using CongTy.ApiClient;
using CongTy.Contracts;

namespace CongTy.Desktop.Printing;

internal static class DocumentPrintTemplateRuntime
{
    private const double A4Width = 793.700787;
    private const double A4Height = 1122.519685;
    private const double A5Width = 559.370079;
    private const double A5Height = 793.700787;

    public static async Task<DocumentPrintTemplateData?> LoadForPrintAsync(
        Window? owner,
        string documentType,
        string templateCode = "standard")
    {
        try
        {
            var app = (App)Application.Current;
            var service = app.ResolveRequired<IDocumentPrintTemplateService>();
            var templates = await service.ListAsync();
            var template = templates.FirstOrDefault(item =>
                string.Equals(item.DocumentType, documentType, StringComparison.OrdinalIgnoreCase)
                && string.Equals(item.TemplateCode, templateCode, StringComparison.OrdinalIgnoreCase));
            return template ?? CreateFallbackTemplate(documentType, templateCode);
        }
        catch (Exception exception)
        {
            Debug.WriteLine(exception);
            return CreateFallbackTemplate(documentType, templateCode);
        }
    }

    public static FlowDocument CreateDocument(
        DocumentPrintTemplateData template,
        double baseFontSize = 11,
        Thickness? padding = null)
    {
        var size = PreferredPageSize(template);
        var pagePadding = padding ?? new Thickness(36);
        return new FlowDocument
        {
            PageWidth = size.Width,
            PageHeight = size.Height,
            PagePadding = pagePadding,
            ColumnGap = 0,
            ColumnWidth = Math.Max(1, size.Width - pagePadding.Left - pagePadding.Right),
            FontFamily = new System.Windows.Media.FontFamily("Segoe UI"),
            FontSize = baseFontSize * Math.Clamp(template.FontSizePercent, 80, 140) / 100d
        };
    }

    public static void PrepareDialog(PrintDialog dialog, DocumentPrintTemplateData template)
    {
        try
        {
            dialog.PrintTicket ??= new PrintTicket();
            dialog.PrintTicket.PageMediaSize = new PageMediaSize(
                string.Equals(template.PageSize, "A5", StringComparison.OrdinalIgnoreCase)
                    ? PageMediaSizeName.ISOA5
                    : PageMediaSizeName.ISOA4);
        }
        catch (Exception exception)
        {
            Debug.WriteLine(exception);
        }
    }

    public static void ApplyPrintableArea(
        FlowDocument document,
        PrintDialog dialog,
        DocumentPrintTemplateData template,
        Thickness? padding = null)
    {
        var preferred = PreferredPageSize(template);
        var pagePadding = padding ?? new Thickness(36);
        var width = IsUsable(dialog.PrintableAreaWidth) ? dialog.PrintableAreaWidth : preferred.Width;
        var height = IsUsable(dialog.PrintableAreaHeight) ? dialog.PrintableAreaHeight : preferred.Height;
        document.PageWidth = width;
        document.PageHeight = height;
        document.PagePadding = pagePadding;
        document.ColumnGap = 0;
        document.ColumnWidth = Math.Max(1, width - pagePadding.Left - pagePadding.Right);
    }

    public static bool Shows(DocumentPrintTemplateData template, string key) =>
        template.VisibleFieldKeys.Contains("*", StringComparer.Ordinal)
        || template.VisibleFieldKeys.Contains(key, StringComparer.Ordinal);

    public static TextAlignment Align(string? value) =>
        value?.Trim().ToLowerInvariant() switch
        {
            "center" => TextAlignment.Center,
            "right" => TextAlignment.Right,
            _ => TextAlignment.Left
        };

    public static void AddHeader(
        FlowDocument document,
        DocumentPrintTemplateData template,
        string defaultTitle,
        string? number,
        string? subtitle = null,
        string? fallbackHeading = null,
        bool breakPageBefore = false)
    {
        var heading = string.IsNullOrWhiteSpace(template.Heading) ? fallbackHeading : template.Heading;
        var firstBlockPending = breakPageBefore;

        if (template.HeadingVisible && !string.IsNullOrWhiteSpace(heading))
        {
            document.Blocks.Add(new Paragraph(new Run(heading.Trim()))
            {
                FontSize = 14,
                FontWeight = FontWeights.SemiBold,
                TextAlignment = Align(template.HeadingAlign),
                Margin = new Thickness(0, 0, 0, 5),
                BreakPageBefore = firstBlockPending
            });
            firstBlockPending = false;
        }

        var title = string.IsNullOrWhiteSpace(template.Title) ? defaultTitle : template.Title.Trim();
        document.Blocks.Add(new Paragraph(new Run(title))
        {
            FontSize = 20,
            FontWeight = FontWeights.Bold,
            TextAlignment = Align(template.TitleAlign),
            Margin = new Thickness(0, 0, 0, 4),
            BreakPageBefore = firstBlockPending
        });

        if (!string.IsNullOrWhiteSpace(number))
        {
            document.Blocks.Add(new Paragraph(new Run($"Số: {number.Trim()}"))
            {
                TextAlignment = Align(template.TitleAlign),
                Margin = new Thickness(0, 0, 0, string.IsNullOrWhiteSpace(subtitle) ? 14 : 3)
            });
        }

        var effectiveSubtitle = string.IsNullOrWhiteSpace(template.Subtitle) ? subtitle : template.Subtitle;
        if (!string.IsNullOrWhiteSpace(effectiveSubtitle))
        {
            document.Blocks.Add(new Paragraph(new Run(effectiveSubtitle.Trim()))
            {
                TextAlignment = Align(template.TitleAlign),
                Margin = new Thickness(0, 0, 0, 14)
            });
        }
    }

    public static void AddSignatures(
        FlowDocument document,
        DocumentPrintTemplateData template,
        params string[] labels)
    {
        if (!Shows(template, "signatures") || labels.Length == 0) return;

        var table = new Table { CellSpacing = 0, Margin = new Thickness(0, 24, 0, 0) };
        foreach (var _ in labels) table.Columns.Add(new TableColumn());
        var group = new TableRowGroup();
        var row = new TableRow();
        foreach (var label in labels)
        {
            row.Cells.Add(new TableCell(new Paragraph(new Run($"{label}\n\n\n(Ký, ghi rõ họ tên)"))
            {
                TextAlignment = TextAlignment.Center,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(4)
            }));
        }
        group.Rows.Add(row);
        table.RowGroups.Add(group);
        document.Blocks.Add(table);
    }

    private static Size PreferredPageSize(DocumentPrintTemplateData template) =>
        string.Equals(template.PageSize, "A5", StringComparison.OrdinalIgnoreCase)
            ? new Size(A5Width, A5Height)
            : new Size(A4Width, A4Height);

    private static bool IsUsable(double value) =>
        value > 0 && !double.IsNaN(value) && !double.IsInfinity(value);

    private static DocumentPrintTemplateData CreateFallbackTemplate(string documentType, string templateCode) =>
        new()
        {
            DocumentType = documentType,
            TemplateCode = templateCode,
            PageSize = string.Equals(documentType, "CUSTOMER_PAYMENT", StringComparison.OrdinalIgnoreCase) ? "A5" : "A4",
            FontSizePercent = 100,
            VisibleFieldKeys = ["*"],
            HeadingVisible = true,
            HeadingAlign = "left",
            TitleAlign = "right"
        };
}
