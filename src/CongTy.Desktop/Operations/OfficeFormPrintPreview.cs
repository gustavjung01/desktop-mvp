using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using CongTy.Contracts;
using CongTy.Desktop.Printing;

namespace CongTy.Desktop.Operations;

internal static class OfficeFormPrintPreview
{
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
            Heading = "CÔNG TY: ______________________________",
            HeadingVisible = true,
            HeadingAlign = "left",
            TitleAlign = "center"
        };
        var document = BuildDocument(form, template);
        var viewer = new FlowDocumentPageViewer
        {
            Document = document,
            Margin = new Thickness(8),
            Zoom = 95,
            MinZoom = 50,
            MaxZoom = 180
        };

        var printButton = new Button
        {
            Content = "In / lưu PDF",
            Padding = new Thickness(14, 7, 14, 7),
            Margin = new Thickness(0, 0, 8, 0),
            MinWidth = 105
        };
        var closeButton = new Button
        {
            Content = "Đóng",
            Padding = new Thickness(14, 7, 14, 7),
            MinWidth = 72
        };
        var actions = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(12, 10, 12, 10)
        };
        actions.Children.Add(printButton);
        actions.Children.Add(closeButton);

        var panel = new DockPanel();
        DockPanel.SetDock(actions, Dock.Top);
        panel.Children.Add(actions);
        panel.Children.Add(viewer);

        var window = new Window
        {
            Title = $"Biểu mẫu văn phòng — {form.Name}",
            Width = 1040,
            Height = 780,
            MinWidth = 820,
            MinHeight = 620,
            WindowStartupLocation = owner is null ? WindowStartupLocation.CenterScreen : WindowStartupLocation.CenterOwner,
            Content = panel
        };
        if (owner?.IsVisible == true) window.Owner = owner;

        printButton.Click += (_, _) => Print(document, template, form.Name);
        closeButton.Click += (_, _) => window.Close();
        window.ShowDialog();
    }

    private static FlowDocument BuildDocument(OfficeFormDefinition form, DocumentPrintTemplateData template)
    {
        var spec = form.Pdf!;
        var document = DocumentPrintTemplateRuntime.CreateDocument(template, 10.5, new Thickness(32));
        DocumentPrintTemplateRuntime.AddHeader(
            document,
            template,
            form.Name.ToUpperInvariant(),
            null,
            "Biểu mẫu văn phòng trống — điền, in và ký ngoài hệ thống");

        if (spec.Meta.Length > 0)
        {
            var info = new Table { CellSpacing = 0, Margin = new Thickness(0, 0, 0, 12) };
            info.Columns.Add(new TableColumn { Width = new GridLength(175) });
            info.Columns.Add(new TableColumn());
            var group = new TableRowGroup();
            foreach (var label in spec.Meta)
            {
                var row = new TableRow();
                row.Cells.Add(Cell(label, true));
                row.Cells.Add(Cell("................................................................................................", false));
                group.Rows.Add(row);
            }
            info.RowGroups.Add(group);
            document.Blocks.Add(info);
        }

        if (spec.Columns is { Length: > 0 } columns)
        {
            var table = new Table { CellSpacing = 0, Margin = new Thickness(0, 4, 0, 10) };
            foreach (var _ in columns) table.Columns.Add(new TableColumn());
            var group = new TableRowGroup();
            var header = new TableRow();
            foreach (var column in columns) header.Cells.Add(Cell(column, true, true));
            group.Rows.Add(header);
            for (var rowIndex = 0; rowIndex < 8; rowIndex++)
            {
                var row = new TableRow();
                for (var columnIndex = 0; columnIndex < columns.Length; columnIndex++)
                    row.Cells.Add(Cell(columnIndex == 0 && columns[columnIndex] == "STT" ? (rowIndex + 1).ToString() : string.Empty, false));
                group.Rows.Add(row);
            }
            table.RowGroups.Add(group);
            document.Blocks.Add(table);
        }
        else
        {
            document.Blocks.Add(new Paragraph(new Run("Nội dung / đề nghị / giải trình:"))
            {
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 10, 0, 4)
            });
            for (var index = 0; index < 6; index++)
                document.Blocks.Add(new Paragraph(new Run("................................................................................................................................................"))
                {
                    Margin = new Thickness(0, 2, 0, 2)
                });
        }

        if (!string.IsNullOrWhiteSpace(spec.Note))
        {
            var note = new Paragraph { Margin = new Thickness(0, 10, 0, 0) };
            note.Inlines.Add(new Run("Ghi chú: ") { FontWeight = FontWeights.SemiBold });
            note.Inlines.Add(new Run(spec.Note));
            document.Blocks.Add(note);
        }

        DocumentPrintTemplateRuntime.AddSignatures(
            document,
            template,
            spec.Signatures is { Length: > 0 } ? spec.Signatures : ["Người lập", "Bộ phận liên quan", "Người xác nhận"]);

        document.Blocks.Add(new Paragraph(new Run("Mẫu trống — không chứa số liệu hiện tại của hệ thống."))
        {
            Margin = new Thickness(0, 18, 0, 0),
            FontSize = 9,
            Foreground = Brushes.Gray,
            TextAlignment = TextAlignment.Center
        });
        return document;
    }

    private static TableCell Cell(string value, bool bold, bool header = false)
    {
        var paragraph = new Paragraph(new Run(value))
        {
            Margin = new Thickness(0),
            FontWeight = bold ? FontWeights.SemiBold : FontWeights.Normal
        };
        return new TableCell(paragraph)
        {
            Padding = new Thickness(5, 4, 5, 4),
            BorderBrush = Brushes.LightGray,
            BorderThickness = header ? new Thickness(0, 0, 0, 1) : new Thickness(0, 0, 0, 0.5)
        };
    }

    private static void Print(FlowDocument document, DocumentPrintTemplateData template, string jobName)
    {
        var dialog = new PrintDialog();
        DocumentPrintTemplateRuntime.PrepareDialog(dialog, template);
        if (dialog.ShowDialog() != true) return;
        DocumentPrintTemplateRuntime.ApplyPrintableArea(document, dialog, template, new Thickness(32));
        dialog.PrintDocument(((IDocumentPaginatorSource)document).DocumentPaginator, jobName);
    }
}
