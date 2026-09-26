using System.Windows;
using CongTy.Contracts;
using CongTy.Desktop.Printing;

namespace CongTy.Desktop.Purchasing;

internal static class SupplierReturnPrintPreview
{
    public static void Print(Window? owner, SupplierReturnData item, DocumentPrintTemplateData template) =>
        ActualDocumentPrintPreview.PrintSupplierReturn(owner, item, template);
}
