using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Threading;
using CongTy.Contracts;
using CongTy.Desktop.Printing;

namespace CongTy.Desktop.Operations;

internal static class OfficeFormPrintPreview
{
    private const string BrandName = "HƯNG PHÁT";
    private const string BlankValue = "................................................................";
    private const string DefaultNote = "Biểu mẫu trống — điền đầy đủ thông tin trước khi ký hoặc sử dụng.";

    private static readonly Brush PaperBrush = Brushes.White;
    private static readonly Brush TextBrush = new SolidColorBrush(Color.FromRgb(0x17, 0x17, 0x17));
    private static readonly Brush SecondaryTextBrush = new SolidColorBrush(Color.FromRgb(0x55, 0x55, 0x55));
    private static readonly Brush RuleBrush = new SolidColorBrush(Color.FromRgb(0x99, 0x99, 0x99));
    private static readonly Brush HeaderFillBrush = new SolidColorBrush(Color.FromRgb(0xF1, 0xF1, 0xF1));
    private static readonly Brush PreviewBackgroundBrush = new SolidColorBrush(Color.FromRgb(0x5F, 0x63, 0x68));
    private static readonly Brush ToolbarBrush = new SolidColorBrush(Color.FromRgb(0xF5, 0xF6, 0xF7));

    public static void Show(Window? owner, OfficeFormDefinition form)
    {
        if (form.Pdf is null) return;

        var template = new DocumentPrintTemplateData
        {
            DocumentType = "OFFICE_FORM",
            TemplateCode = form.Id,
            Name = form.Name,
            PageSize = form.Pdf.PageSize,
            FontSizePercent = 100,
            VisibleFieldKeys = ["*"],
            Heading = BrandName,
            HeadingVisible = true,
            HeadingAlign = "left",
            TitleAlign = "right"
        };

        var document = BuildDocument(form, template);
        var viewer = new FlowDocumentPageViewer
        {
            Document = document,
            Background = PreviewBackgroundBrush,
            BorderThickness = new Thickness(0),
            Margin = new Thickness(0),
            MinZoom = 45,
            MaxZoom = 180,
            ZoomIncrement = 10
        };

        var printButton = new Button
        {
            Content = "In / lưu PDF",
            Padding = new Thickness(16, 8, 16, 8),
            Margin = new Thickness(0, 0, 8, 0),
            MinWidth = 112
        };
        var closeButton = new Button
        {
            Content = "Đóng",
            Padding = new Thickness(16, 8, 16, 8),
            MinWidth = 76
        };
        var actions = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right
        };
        actions.Children.Add(printButton);
        actions.Children.Add(closeButton);

        var toolbar = new Border
        {
            Background = ToolbarBrush,
            BorderBrush = new SolidColorBrush(Color.FromRgb(0xD7, 0xDA, 0xDD)),
            BorderThickness = new Thickness(0, 0, 0, 1),
            Padding = new Thickness(14, 10, 14, 10),
            Child = actions
        };

        var panel = new DockPanel { Background = PreviewBackgroundBrush };
        DockPanel.SetDock(toolbar, Dock.Top);
        panel.Children.Add(toolbar);
        panel.Children.Add(viewer);

        var window = new Window
        {
            Title = $"Biểu mẫu văn phòng — {form.Name}",
            Width = 1180,
            Height = 860,
            MinWidth = 900,
            MinHeight = 680,
            Background = PreviewBackgroundBrush,
            WindowStartupLocation = owner is null ? WindowStartupLocation.CenterScreen : WindowStartupLocation.CenterOwner,
            Content = panel
        };
        if (owner?.IsVisible == true) window.Owner = owner;

        viewer.Loaded += (_, _) => FitPreview(viewer, document);
        viewer.SizeChanged += (_, _) => FitPreview(viewer, document);
        printButton.Click += (_, _) => Print(document, template, form.Name);
        closeButton.Click += (_, _) => window.Close();
        window.ShowDialog();
    }

    private static FlowDocument BuildDocument(OfficeFormDefinition form, DocumentPrintTemplateData template)
    {
        var spec = form.Pdf!;
        var document = DocumentPrintTemplateRuntime.CreateDocument(
            template,
            Pt(10.5),
            OfficePagePadding(template.PageSize));

        document.FontFamily = new FontFamily("Arial");
        document.FontSize = Pt(10.5);
        document.Foreground = TextBrush;
        document.Background = PaperBrush;
        document.LineHeight = Pt(10.5) * 1.38;

        AddOfficeHeader(document, form.Name);
        AddMetaGrid(document, spec.Meta);

        if (spec.Columns is { Length: > 0 } columns)
            AddBlankTable(document, columns);

        AddNote(document, spec.Note);
        AddOfficeSignatures(
            document,
            spec.Signatures is { Length: > 0 }
                ? spec.Signatures
                : ["Người lập", "Bộ phận liên quan", "Người xác nhận"]);

        return document;
    }

    private static void AddOfficeHeader(FlowDocument document, string title)
    {
        var table = new Table
        {
            CellSpacing = 0,
            Margin = new Thickness(0, 0, 0, Pt(9))
        };
        table.Columns.Add(new TableColumn { Width = new GridLength(0.42, GridUnitType.Star) });
        table.Columns.Add(new TableColumn { Width = new GridLength(0.58, GridUnitType.Star) });

        var row = new TableRow();
        var brandCell = HeaderCell();
        brandCell.Blocks.Add(new Paragraph(new Run(BrandName))
        {
            FontSize = Pt(16),
            FontWeight = FontWeights.Bold,
            Margin = new Thickness(0),
            LineHeight = Pt(18)
        });
        brandCell.Blocks.Add(new Paragraph(new Run("Biểu mẫu văn phòng trống"))
        {
            FontSize = Pt(8.5),
            Foreground = SecondaryTextBrush,
            Margin = new Thickness(0, Pt(2), 0, 0),
            LineHeight = Pt(10)
        });

        var titleCell = HeaderCell();
        titleCell.Blocks.Add(new Paragraph(new Run(title))
        {
            FontSize = Pt(18),
            FontWeight = FontWeights.Bold,
            TextAlignment = TextAlignment.Right,
            Margin = new Thickness(0, Pt(15), 0, 0),
            LineHeight = Pt(20)
        });

        row.Cells.Add(brandCell);
        row.Cells.Add(titleCell);
        var group = new TableRowGroup();
        group.Rows.Add(row);
        table.RowGroups.Add(group);
        document.Blocks.Add(table);
    }

    private static TableCell HeaderCell() =>
        new()
        {
            Padding = new Thickness(0, 0, 0, Pt(7.5)),
            BorderBrush = new SolidColorBrush(Color.FromRgb(0x24, 0x24, 0x24)),
            BorderThickness = new Thickness(0, 0, 0, 2)
        };

    private static void AddMetaGrid(FlowDocument document, string[] meta)
    {
        if (meta.Length == 0) return;

        var table = new Table
        {
            CellSpacing = 0,
            Margin = new Thickness(0, 0, 0, Pt(9.5))
        };
        table.Columns.Add(new TableColumn { Width = new GridLength(0.19, GridUnitType.Star) });
        table.Columns.Add(new TableColumn { Width = new GridLength(0.31, GridUnitType.Star) });
        table.Columns.Add(new TableColumn { Width = new GridLength(0.19, GridUnitType.Star) });
        table.Columns.Add(new TableColumn { Width = new GridLength(0.31, GridUnitType.Star) });

        var group = new TableRowGroup();
        for (var index = 0; index < meta.Length; index += 2)
        {
            var row = new TableRow();
            AddMetaPair(row, meta[index]);
            AddMetaPair(row, index + 1 < meta.Length ? meta[index + 1] : string.Empty);
            group.Rows.Add(row);
        }

        table.RowGroups.Add(group);
        document.Blocks.Add(table);
    }

    private static void AddMetaPair(TableRow row, string label)
    {
        row.Cells.Add(MetaCell(label, isLabel: true));
        row.Cells.Add(MetaCell(string.IsNullOrWhiteSpace(label) ? string.Empty : BlankValue, isLabel: false));
    }

    private static TableCell MetaCell(string value, bool isLabel)
    {
        var paragraph = new Paragraph(new Run(value))
        {
            FontSize = Pt(9),
            Foreground = isLabel ? SecondaryTextBrush : TextBrush,
            FontWeight = isLabel ? FontWeights.Normal : FontWeights.SemiBold,
            Margin = new Thickness(0),
            LineHeight = Pt(11)
        };

        return new TableCell(paragraph)
        {
            Padding = new Thickness(Pt(2), Pt(1.5), Pt(4), Pt(2.5)),
            BorderBrush = new SolidColorBrush(Color.FromRgb(0xAA, 0xAA, 0xAA)),
            BorderThickness = new Thickness(0, 0, 0, 0.7)
        };
    }

    private static void AddBlankTable(FlowDocument document, string[] columns)
    {
        var table = new Table
        {
            CellSpacing = 0,
            Margin = new Thickness(0, Pt(5), 0, Pt(7.5))
        };

        foreach (var column in columns)
            table.Columns.Add(new TableColumn { Width = new GridLength(ColumnWeight(column), GridUnitType.Star) });

        var group = new TableRowGroup();
        var header = new TableRow();
        foreach (var column in columns)
            header.Cells.Add(GridCell(column, bold: true, header: true, center: ShouldCenter(column)));
        group.Rows.Add(header);

        for (var rowIndex = 0; rowIndex < 8; rowIndex++)
        {
            var row = new TableRow();
            for (var columnIndex = 0; columnIndex < columns.Length; columnIndex++)
            {
                var value = columnIndex == 0 && columns[columnIndex] == "STT"
                    ? (rowIndex + 1).ToString()
                    : string.Empty;
                row.Cells.Add(GridCell(value, bold: false, header: false, center: ShouldCenter(columns[columnIndex])));
            }
            group.Rows.Add(row);
        }

        table.RowGroups.Add(group);
        document.Blocks.Add(table);
    }

    private static TableCell GridCell(string value, bool bold, bool header, bool center)
    {
        var paragraph = new Paragraph(new Run(value))
        {
            FontSize = Pt(header ? 9 : 9.3),
            FontWeight = bold ? FontWeights.SemiBold : FontWeights.Normal,
            TextAlignment = center ? TextAlignment.Center : TextAlignment.Left,
            Margin = new Thickness(0),
            LineHeight = Pt(header ? 11.3 : 11.8)
        };

        return new TableCell(paragraph)
        {
            Padding = new Thickness(Pt(4.5), Pt(3.4), Pt(4.5), Pt(3.4)),
            Background = header ? HeaderFillBrush : PaperBrush,
            BorderBrush = RuleBrush,
            BorderThickness = new Thickness(0.8)
        };
    }

    private static double ColumnWeight(string label)
    {
        if (string.Equals(label, "STT", StringComparison.Ordinal)) return 0.5;
        if (label.Contains("Đã thanh toán", StringComparison.OrdinalIgnoreCase)) return 1.55;
        if (label.Contains("Chuyến / phiếu", StringComparison.OrdinalIgnoreCase)) return 1.45;
        if (label.Contains("Khách hàng", StringComparison.OrdinalIgnoreCase)) return 1.35;
        if (label.Contains("Tên hàng", StringComparison.OrdinalIgnoreCase)) return 1.25;
        if (label.Contains("Ghi chú", StringComparison.OrdinalIgnoreCase)) return 1.15;
        return 1.0;
    }

    private static bool ShouldCenter(string label) =>
        string.Equals(label, "STT", StringComparison.Ordinal)
        || label.Contains("Số lượng", StringComparison.OrdinalIgnoreCase)
        || label.Contains("Tiền", StringComparison.OrdinalIgnoreCase);

    private static void AddNote(FlowDocument document, string? noteText)
    {
        var paragraph = new Paragraph
        {
            Margin = new Thickness(0),
            FontSize = Pt(9.3),
            LineHeight = Pt(11.5)
        };
        paragraph.Inlines.Add(new Run("Ghi chú: ") { FontWeight = FontWeights.Bold });
        paragraph.Inlines.Add(new Run(string.IsNullOrWhiteSpace(noteText) ? DefaultNote : noteText.Trim()));

        document.Blocks.Add(new Section(paragraph)
        {
            BorderBrush = new SolidColorBrush(Color.FromRgb(0xBB, 0xBB, 0xBB)),
            BorderThickness = new Thickness(1),
            Padding = new Thickness(Pt(7.5), Pt(5.5), Pt(7.5), Pt(5.5)),
            Margin = new Thickness(0, Pt(2), 0, Pt(12))
        });
    }

    private static void AddOfficeSignatures(FlowDocument document, string[] labels)
    {
        if (labels.Length == 0) return;

        var table = new Table
        {
            CellSpacing = 0,
            Margin = new Thickness(0, Pt(3), 0, 0)
        };
        foreach (var _ in labels)
            table.Columns.Add(new TableColumn());

        var row = new TableRow();
        foreach (var label in labels)
        {
            var cell = new TableCell
            {
                Padding = new Thickness(Pt(5), Pt(2), Pt(5), Pt(2))
            };
            cell.Blocks.Add(new Paragraph(new Run(label))
            {
                FontWeight = FontWeights.Bold,
                TextAlignment = TextAlignment.Center,
                FontSize = Pt(9.3),
                Margin = new Thickness(0)
            });
            cell.Blocks.Add(new Paragraph(new Run("(Ký, ghi rõ họ tên)"))
            {
                Foreground = SecondaryTextBrush,
                TextAlignment = TextAlignment.Center,
                FontSize = Pt(8.3),
                Margin = new Thickness(0, Pt(2.5), 0, 0)
            });
            cell.Blocks.Add(new Paragraph(new Run("\n\n"))
            {
                FontSize = Pt(9),
                Margin = new Thickness(0)
            });
            row.Cells.Add(cell);
        }

        var group = new TableRowGroup();
        group.Rows.Add(row);
        table.RowGroups.Add(group);
        document.Blocks.Add(table);
    }

    private static void FitPreview(FlowDocumentPageViewer viewer, FlowDocument document)
    {
        if (viewer.ActualWidth <= 0 || viewer.ActualHeight <= 0) return;

        viewer.Dispatcher.BeginInvoke(() =>
        {
            var widthRatio = Math.Max(0.1, (viewer.ActualWidth - 72) / document.PageWidth);
            var heightRatio = Math.Max(0.1, (viewer.ActualHeight - 100) / document.PageHeight);
            viewer.Zoom = Math.Clamp(Math.Min(widthRatio, heightRatio) * 100d, viewer.MinZoom, viewer.MaxZoom);
        }, DispatcherPriority.Loaded);
    }

    private static Thickness OfficePagePadding(string? pageSize) =>
        string.Equals(pageSize, "A5", StringComparison.OrdinalIgnoreCase)
            ? new Thickness(Mm(8), Mm(9), Mm(8), Mm(7))
            : new Thickness(Mm(10), Mm(11), Mm(10), Mm(9));

    private static double Pt(double points) => points * 96d / 72d;
    private static double Mm(double millimeters) => millimeters * 96d / 25.4d;

    private static void Print(FlowDocument document, DocumentPrintTemplateData template, string jobName)
    {
        var dialog = new PrintDialog();
        DocumentPrintTemplateRuntime.PrepareDialog(dialog, template);
        if (dialog.ShowDialog() != true) return;

        DocumentPrintTemplateRuntime.ApplyPrintableArea(
            document,
            dialog,
            template,
            OfficePagePadding(template.PageSize));
        dialog.PrintDocument(((IDocumentPaginatorSource)document).DocumentPaginator, jobName);
    }
}
