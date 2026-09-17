using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using CongTy.Contracts;

namespace CongTy.Desktop.Sales;

internal static class SalesOrderPrintPreview
{
    private const double DefaultColumnWidth=720;

    public static void Show(Window? owner,SalesOrderData order,SalesOrderVersionData version)
    {
        try
        {
            ShowCore(owner,order,version);
        }
        catch(Exception exception)
        {
            Debug.WriteLine(exception);
            ShowWarning(owner,"Không mở được bản xem trước của đơn bán hàng. Vui lòng thử lại.");
        }
    }

    public static void ShowBatch(Window? owner,IReadOnlyList<(SalesOrderData Order,SalesOrderVersionData Version)> items)
    {
        if(items.Count==0)return;
        try
        {
            var document=BuildBatchDocument(items);
            var viewer=new FlowDocumentPageViewer { Document=document,Margin=new Thickness(8) };
            var printButton=new Button { Content="In…",MinWidth=90,Margin=new Thickness(4) };
            var closeButton=new Button { Content="Đóng",MinWidth=90,Margin=new Thickness(4) };
            var actions=new StackPanel { Orientation=Orientation.Horizontal,HorizontalAlignment=HorizontalAlignment.Right,Margin=new Thickness(8,6,8,2) };
            actions.Children.Add(printButton); actions.Children.Add(closeButton);
            var layout=new DockPanel();
            DockPanel.SetDock(actions,Dock.Top); layout.Children.Add(actions); layout.Children.Add(viewer);
            var window=new Window
            {
                Title=$"Xem trước in · {items.Count:N0} đơn",
                Width=980,Height=760,MinWidth=760,MinHeight=560,
                Content=layout,WindowStartupLocation=WindowStartupLocation.CenterOwner
            };
            if(owner is not null)window.Owner=owner;
            printButton.Click+=(_,_)=>TryPrintBatch(window,document,items.Count);
            closeButton.Click+=(_,_)=>window.Close();
            window.ShowDialog();
        }
        catch(Exception exception)
        {
            Debug.WriteLine(exception);
            ShowWarning(owner,"Không mở được bản xem trước các đơn đã chọn. Vui lòng thử lại.");
        }
    }

    private static void ShowCore(Window? owner,SalesOrderData order,SalesOrderVersionData version)
    {
        var document=BuildDocument(order,version);
        var viewer=new FlowDocumentPageViewer { Document=document,Margin=new Thickness(8) };
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
        printButton.Click+=(_,_)=>TryPrint(window,document,order.Number);
        closeButton.Click+=(_,_)=>window.Close();
        window.ShowDialog();
    }

    private static FlowDocument BuildDocument(SalesOrderData order,SalesOrderVersionData version)
    {
        var document=CreateDocument();
        AppendOrder(document,order,version,false);
        return document;
    }

    private static FlowDocument BuildBatchDocument(IReadOnlyList<(SalesOrderData Order,SalesOrderVersionData Version)> items)
    {
        var document=CreateDocument();
        for(var index=0;index<items.Count;index++)
        {
            var item=items[index];
            AppendOrder(document,item.Order,item.Version,index>0);
        }
        return document;
    }

    private static FlowDocument CreateDocument()=>new()
    {
        FontFamily=new FontFamily("Segoe UI"),
        FontSize=11,
        PagePadding=new Thickness(36),
        ColumnWidth=DefaultColumnWidth
    };

    private static void AppendOrder(FlowDocument document,SalesOrderData order,SalesOrderVersionData version,bool pageBreak)
    {
        document.Blocks.Add(new Paragraph(new Run("PHIẾU XUẤT KHO"))
        {
            FontSize=22,FontWeight=FontWeights.SemiBold,TextAlignment=TextAlignment.Center,
            Margin=new Thickness(0,0,0,4),BreakPageBefore=pageBreak
        });
        document.Blocks.Add(new Paragraph(new Run($"Số đơn: {SalesPresentation.Number(order.Number)}")) { TextAlignment=TextAlignment.Center,Margin=new Thickness(0,0,0,14) });

        var customer=version.CustomerMode=="WALK_IN"
            ? Display(version.WalkInDisplayName,"Khách vãng lai")
            : Display(version.CustomerName);
        var customerCode=version.CustomerMode=="WALK_IN"?"Khách vãng lai":Display(version.CustomerCode);
        var phone=version.CustomerMode=="WALK_IN"?version.WalkInPhone:version.CustomerAddress?.Phone;
        AddInfo(document,"Khách hàng",customer);
        AddInfo(document,"Mã khách",customerCode);
        AddInfo(document,"Điện thoại",Display(phone));
        AddInfo(document,"Địa chỉ",Address(version.CustomerAddress));
        AddInfo(document,"Ngày đơn",Date(version.ConfirmedAt??version.CreatedAt));
        AddInfo(document,"Khối lượng",string.IsNullOrWhiteSpace(version.TotalWeightKg)?"Chưa đủ dữ liệu":$"{SalesPresentation.Quantity(version.TotalWeightKg)} kg");
        AddInfo(document,"Kho",JoinCodeName(version.WarehouseCode,version.WarehouseName));
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
        if(!string.IsNullOrWhiteSpace(version.Note))document.Blocks.Add(new Paragraph(new Run($"Ghi chú: {version.Note.Trim()}")) { Margin=new Thickness(0,10,0,0) });

        var signatures=new Table { CellSpacing=0,Margin=new Thickness(0,24,0,0) };
        signatures.Columns.Add(new TableColumn());signatures.Columns.Add(new TableColumn());signatures.Columns.Add(new TableColumn());
        var group=new TableRowGroup();var row=new TableRow();
        foreach(var label in new[]{"Người lập","Kho giao hàng","Khách hàng"})row.Cells.Add(new TableCell(new Paragraph(new Run(label)){TextAlignment=TextAlignment.Center,FontWeight=FontWeights.SemiBold}){Padding=new Thickness(6)});
        group.Rows.Add(row);signatures.RowGroups.Add(group);document.Blocks.Add(signatures);
    }

    private static Table BuildLines(SalesOrderVersionData version)
    {
        var lines=version.Lines??[];
        var showDiscount=lines.Any(x=>!IsZero(x.DiscountAmount))||!IsZero(version.DiscountTotal);
        var showTax=lines.Any(x=>!IsZero(x.TaxAmount))||!IsZero(version.TaxTotal);
        var table=new Table { CellSpacing=0,Margin=new Thickness(0,14,0,0) };
        var headers=new List<string>{"STT","Tên sản phẩm","SKU","SL","ĐVT","Đơn giá"};
        if(showDiscount)headers.Add("CK"); if(showTax)headers.Add("Thuế"); headers.Add("Thành tiền");
        foreach(var _ in headers)table.Columns.Add(new TableColumn());
        var group=new TableRowGroup();var header=new TableRow();
        foreach(var value in headers)header.Cells.Add(Cell(value,true));
        group.Rows.Add(header);
        foreach(var line in lines.OrderBy(x=>x.LineNumber))
        {
            var row=new TableRow();
            row.Cells.Add(Cell(line.LineNumber.ToString(CultureInfo.InvariantCulture)));
            row.Cells.Add(Cell(Display(line.ItemName)));row.Cells.Add(Cell(Display(line.Sku)));
            row.Cells.Add(Cell(SalesPresentation.Quantity(line.Quantity)));
            row.Cells.Add(Cell(Display(line.UnitName??line.UnitCode)));
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
        var run=new Run(Display(value)); if(bold)run.FontWeight=FontWeights.SemiBold;
        return new TableCell(new Paragraph(run){Margin=new Thickness(0)}) { BorderBrush=Brushes.Gray,BorderThickness=new Thickness(0.5),Padding=new Thickness(5) };
    }

    private static void AddInfo(FlowDocument document,string label,string? value) =>
        document.Blocks.Add(new Paragraph { Margin=new Thickness(0,1,0,1),Inlines={new Bold(new Run($"{label}: ")),new Run(Display(value))} });

    private static string Address(SalesOrderAddressData? address)
    {
        if(address is null)return "—";
        var parts=new[]{address.AddressLine1,address.AddressLine2,address.Ward,address.District,address.Province}.Where(x=>!string.IsNullOrWhiteSpace(x)).Select(x=>x!.Trim());
        var value=string.Join(", ",parts);
        return Display(value);
    }

    private static string JoinCodeName(string? code,string? name)
    {
        var parts=new[]{code,name}.Where(x=>!string.IsNullOrWhiteSpace(x)).Select(x=>x!.Trim());
        return Display(string.Join(" · ",parts));
    }

    private static string Display(string? value,string fallback="—") => string.IsNullOrWhiteSpace(value)?fallback:value.Trim();

    private static string Date(string? value)
    {
        if(string.IsNullOrWhiteSpace(value))return "—";
        return DateTimeOffset.TryParse(value,out var parsed)?parsed.ToLocalTime().ToString("dd/MM/yyyy",CultureInfo.GetCultureInfo("vi-VN")):value.Trim();
    }

    private static bool IsZero(string? value) => decimal.TryParse(value,NumberStyles.Number,CultureInfo.InvariantCulture,out var amount)&&amount==0;

    private static void TryPrint(Window owner,FlowDocument document,string? number)
    {
        try
        {
            Print(document,number);
        }
        catch(Exception exception)
        {
            Debug.WriteLine(exception);
            ShowWarning(owner,"Không in được đơn bán hàng. Vui lòng kiểm tra máy in rồi thử lại.");
        }
    }

    private static void TryPrintBatch(Window owner,FlowDocument document,int count)
    {
        try
        {
            PrintBatch(document,count);
        }
        catch(Exception exception)
        {
            Debug.WriteLine(exception);
            ShowWarning(owner,"Không in được các đơn đã chọn. Vui lòng kiểm tra máy in rồi thử lại.");
        }
    }

    private static void Print(FlowDocument document,string? number)
    {
        var dialog=new PrintDialog();
        if(dialog.ShowDialog()!=true)return;
        ApplyPrintableArea(document,dialog);
        dialog.PrintDocument(((IDocumentPaginatorSource)document).DocumentPaginator,$"Phiếu xuất kho {SalesPresentation.Number(number)}");
    }

    private static void PrintBatch(FlowDocument document,int count)
    {
        var dialog=new PrintDialog();
        if(dialog.ShowDialog()!=true)return;
        ApplyPrintableArea(document,dialog);
        dialog.PrintDocument(((IDocumentPaginatorSource)document).DocumentPaginator,$"Phiếu xuất kho · {count:N0} đơn");
    }

    private static void ApplyPrintableArea(FlowDocument document,PrintDialog dialog)
    {
        document.PagePadding=new Thickness(36);
        if(IsUsablePageSize(dialog.PrintableAreaWidth))
        {
            document.PageWidth=dialog.PrintableAreaWidth;
            document.ColumnWidth=Math.Max(1,dialog.PrintableAreaWidth-document.PagePadding.Left-document.PagePadding.Right);
        }
        if(IsUsablePageSize(dialog.PrintableAreaHeight))document.PageHeight=dialog.PrintableAreaHeight;
    }

    private static bool IsUsablePageSize(double value) => value>0&&!double.IsNaN(value)&&!double.IsInfinity(value);

    private static void ShowWarning(Window? owner,string message)
    {
        if(owner is not null)
        {
            MessageBox.Show(owner,message,"In đơn bán hàng",MessageBoxButton.OK,MessageBoxImage.Warning);
            return;
        }
        MessageBox.Show(message,"In đơn bán hàng",MessageBoxButton.OK,MessageBoxImage.Warning);
    }
}
