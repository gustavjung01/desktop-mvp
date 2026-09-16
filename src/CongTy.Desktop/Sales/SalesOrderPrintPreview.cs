using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using CongTy.Contracts;

namespace CongTy.Desktop.Sales;

internal static class SalesOrderPrintPreview
{
    public static void Show(Window? owner,SalesOrderData order,SalesOrderVersionData version)
    {
        var document=BuildDocument(order,version);
        var viewer=new DocumentViewer { Document=document,Margin=new Thickness(8) };
        var printButton=new Button { Content="In…",MinWidth=90,Margin=new Thickness(4) };
        var closeButton=new Button { Content="Đóng",MinWidth=90,Margin=new Thickness(4) };
        var actions=new StackPanel { Orientation=Orientation.Horizontal,HorizontalAlignment=HorizontalAlignment.Right,Margin=new Thickness(8,6,8,2) };
        actions.Children.Add(printButton); actions.Children.Add(closeButton);
        var layout=new DockPanel();
        DockPanel.SetDock(actions,Dock.Top); layout.Children.Add(actions); layout.Children.Add(viewer);
        var window=new Window
        {
            Title=$"Xem trước in · {SalesPresentation.Number(order.Number)}",
            Width=980,Height=760,MinWidth=760,MinHeight=560,
            Content=layout,WindowStartupLocation=WindowStartupLocation.CenterOwner
        };
        if(owner is not null)window.Owner=owner;
        printButton.Click+=(_,_)=>Print(document,order.Number);
        closeButton.Click+=(_,_)=>window.Close();
        window.ShowDialog();
    }

    private static FlowDocument BuildDocument(SalesOrderData order,SalesOrderVersionData version)
    {
        var document=new FlowDocument
        {
            FontFamily=new FontFamily("Segoe UI"),
            FontSize=11,
            PagePadding=new Thickness(36),
            ColumnWidth=double.PositiveInfinity
        };
        document.Blocks.Add(new Paragraph(new Run("PHIẾU XUẤT KHO")) { FontSize=22,FontWeight=FontWeights.SemiBold,TextAlignment=TextAlignment.Center,Margin=new Thickness(0,0,0,4) });
        document.Blocks.Add(new Paragraph(new Run($"Số đơn: {SalesPresentation.Number(order.Number)}")) { TextAlignment=TextAlignment.Center,Margin=new Thickness(0,0,0,14) });

        var customer=version.CustomerMode=="WALK_IN"?(version.WalkInDisplayName??version.CustomerName):version.CustomerName;
        var phone=version.CustomerMode=="WALK_IN"?version.WalkInPhone:version.CustomerAddress?.Phone;
        AddInfo(document,"Khách hàng",customer);
        AddInfo(document,"Mã khách",version.CustomerCode);
        AddInfo(document,"Điện thoại",string.IsNullOrWhiteSpace(phone)?"—":phone);
        AddInfo(document,"Địa chỉ",Address(version.CustomerAddress));
        AddInfo(document,"Ngày đơn",Date(version.ConfirmedAt??version.CreatedAt));
        AddInfo(document,"Khối lượng",string.IsNullOrWhiteSpace(version.TotalWeightKg)?"Chưa đủ dữ liệu":$"{SalesPresentation.Quantity(version.TotalWeightKg)} kg");
        AddInfo(document,"Kho",$"{version.WarehouseCode} · {version.WarehouseName}");
        AddInfo(document,"Hình thức giao nhận",version.DeliveryMode=="PICKUP"?"Mua tại quầy":version.DeliveryExecutionMode=="MANUAL"?"Giao thủ công":"Giao theo chuyến");
        AddInfo(document,"Thanh toán",SalesPresentation.CollectionPolicy(version.CollectionPolicy));
        AddInfo(document,"Ngày giao dự kiến",Date(version.RequestedDeliveryDate));

        document.Blocks.Add(BuildLines(version));
        var totals=new Paragraph { TextAlignment=TextAlignment.Right,Margin=new Thickness(0,10,0,0) };
        totals.Inlines.Add(new Run($"Tạm tính: {SalesPresentation.Money(version.Subtotal)}\n"));
        if(!IsZero(version.DiscountTotal))totals.Inlines.Add(new Run($"Chiết khấu: {SalesPresentation.Money(version.DiscountTotal)}\n"));
        if(!IsZero(version.TaxTotal))totals.Inlines.Add(new Run($"Thuế: {SalesPresentation.Money(version.TaxTotal)}\n"));
        totals.Inlines.Add(new Run($"TỔNG CỘNG: {SalesPresentation.Money(version.Total)}") { FontWeight=FontWeights.Bold,FontSize=13 });
        document.Blocks.Add(totals);
        if(!string.IsNullOrWhiteSpace(version.Note))document.Blocks.Add(new Paragraph(new Run($"Ghi chú: {version.Note}")) { Margin=new Thickness(0,10,0,0) });

        var signatures=new Table { CellSpacing=0,Margin=new Thickness(0,24,0,0) };
        signatures.Columns.Add(new TableColumn());signatures.Columns.Add(new TableColumn());signatures.Columns.Add(new TableColumn());
        var group=new TableRowGroup();var row=new TableRow();
        foreach(var label in new[]{"Người lập","Kho giao hàng","Khách hàng"})row.Cells.Add(new TableCell(new Paragraph(new Run(label)){TextAlignment=TextAlignment.Center,FontWeight=FontWeights.SemiBold}){Padding=new Thickness(6)});
        group.Rows.Add(row);signatures.RowGroups.Add(group);document.Blocks.Add(signatures);
        return document;
    }

    private static Table BuildLines(SalesOrderVersionData version)
    {
        var showDiscount=version.Lines.Any(x=>!IsZero(x.DiscountAmount))||!IsZero(version.DiscountTotal);
        var showTax=version.Lines.Any(x=>!IsZero(x.TaxAmount))||!IsZero(version.TaxTotal);
        var table=new Table { CellSpacing=0,Margin=new Thickness(0,14,0,0) };
        var headers=new List<string>{"STT","Tên sản phẩm","SKU","SL","ĐVT","Đơn giá"};
        if(showDiscount)headers.Add("CK"); if(showTax)headers.Add("Thuế"); headers.Add("Thành tiền");
        foreach(var _ in headers)table.Columns.Add(new TableColumn());
        var group=new TableRowGroup();var header=new TableRow();
        foreach(var value in headers)header.Cells.Add(Cell(value,true));
        group.Rows.Add(header);
        foreach(var line in version.Lines.OrderBy(x=>x.LineNumber))
        {
            var row=new TableRow();
            row.Cells.Add(Cell(line.LineNumber.ToString(CultureInfo.InvariantCulture)));
            row.Cells.Add(Cell(line.ItemName));row.Cells.Add(Cell(line.Sku));
            row.Cells.Add(Cell(SalesPresentation.Quantity(line.Quantity)));
            row.Cells.Add(Cell(line.UnitName??line.UnitCode));
            row.Cells.Add(Cell(SalesPresentation.Money(line.UnitPrice)));
            if(showDiscount)row.Cells.Add(Cell(SalesPresentation.Money(line.DiscountAmount)));
            if(showTax)row.Cells.Add(Cell(SalesPresentation.Money(line.TaxAmount)));
            row.Cells.Add(Cell(SalesPresentation.Money(line.LineTotal),true));
            group.Rows.Add(row);
        }
        table.RowGroups.Add(group);return table;
    }

    private static TableCell Cell(string value,bool bold=false)
    {
        var run=new Run(value); if(bold)run.FontWeight=FontWeights.SemiBold;
        return new TableCell(new Paragraph(run){Margin=new Thickness(0)}) { BorderBrush=Brushes.Gray,BorderThickness=new Thickness(0.5),Padding=new Thickness(5) };
    }

    private static void AddInfo(FlowDocument document,string label,string value) =>
        document.Blocks.Add(new Paragraph { Margin=new Thickness(0,1,0,1),Inlines={new Bold(new Run($"{label}: ")),new Run(value)} });

    private static string Address(SalesOrderAddressData? address)
    {
        if(address is null)return "—";
        var parts=new[]{address.AddressLine1,address.AddressLine2,address.Ward,address.District,address.Province}.Where(x=>!string.IsNullOrWhiteSpace(x));
        var value=string.Join(", ",parts);
        return string.IsNullOrWhiteSpace(value)?"—":value;
    }

    private static string Date(string? value)
    {
        if(string.IsNullOrWhiteSpace(value))return "—";
        return DateTimeOffset.TryParse(value,out var parsed)?parsed.ToLocalTime().ToString("dd/MM/yyyy",CultureInfo.GetCultureInfo("vi-VN")):value;
    }

    private static bool IsZero(string? value) => decimal.TryParse(value,NumberStyles.Number,CultureInfo.InvariantCulture,out var amount)&&amount==0;

    private static void Print(FlowDocument document,string? number)
    {
        var dialog=new PrintDialog();
        if(dialog.ShowDialog()!=true)return;
        document.PageWidth=dialog.PrintableAreaWidth;
        document.PageHeight=dialog.PrintableAreaHeight;
        document.PagePadding=new Thickness(36);
        document.ColumnWidth=double.PositiveInfinity;
        dialog.PrintDocument(((IDocumentPaginatorSource)document).DocumentPaginator,$"Phiếu xuất kho {SalesPresentation.Number(number)}");
    }
}