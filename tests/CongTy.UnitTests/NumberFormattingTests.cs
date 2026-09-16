using CongTy.Desktop;
using CongTy.Desktop.Organization;
using CongTy.Desktop.Partners;
using CongTy.Desktop.Sales;

namespace CongTy.UnitTests;

[TestClass]
public sealed class NumberFormattingTests
{
    [TestMethod]
    public void Compact_RemovesTrailingZeroesAcrossDesktopPresentations()
    {
        Assert.AreEqual("72",OfficeNumberFormatting.Compact("72.000000"));
        Assert.AreEqual("1,25",OfficeNumberFormatting.Compact("1.250000"));
        Assert.AreEqual("0%",OfficeNumberFormatting.Percent("0.000000"));
        Assert.AreEqual("12,5",PartnerPresentation.FormatDecimal("12.500000"));
        Assert.AreEqual("9,125",OrganizationPresentation.FormatDecimal("9.125000"));
    }

    [TestMethod]
    public void SalesListNumber_ShowsCompactOfficeNumber()
    {
        Assert.AreEqual("SO00242",SalesPresentation.ListNumber("SO-202609-000242"));
        Assert.AreEqual("NHÁP",SalesPresentation.ListNumber(null));
    }
}
