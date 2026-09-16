using System.IO;
using System.Globalization;
using System.Text;
using System.Text.Json;
using CongTy.Contracts;

namespace CongTy.Desktop.Sales;

public sealed class SalesSkuLocalCatalog
{
    private sealed record PersistedCatalog(string? Cursor,SalesOrderSkuCatalogRowData[] Rows);
    private static readonly JsonSerializerOptions JsonOptions=new(JsonSerializerDefaults.Web);
    private readonly Dictionary<string,SalesOrderSkuCatalogRowData> _rows=new(StringComparer.Ordinal);
    private readonly string _path;
    private bool _loaded;

    public SalesSkuLocalCatalog(string? path=null)
    {
        _path=path??Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"CongTy","Desktop","sales-sku-catalog-v2.json");
    }

    public string? Cursor { get; private set; }
    public int Count=>_rows.Count;

    public async Task LoadAsync(CancellationToken cancellationToken=default)
    {
        if(_loaded)return; _loaded=true;
        try
        {
            if(!File.Exists(_path))return;
            await using var stream=File.OpenRead(_path);
            var saved=await JsonSerializer.DeserializeAsync<PersistedCatalog>(stream,JsonOptions,cancellationToken).ConfigureAwait(false);
            if(saved is null)return; Cursor=saved.Cursor;
            foreach(var row in saved.Rows.Where(IsValidRow))_rows[row.Id]=row;
        }
        catch(IOException){ } catch(UnauthorizedAccessException){ } catch(JsonException){ }
    }

    public async Task ApplyAsync(SalesOrderSkuCatalogData payload,CancellationToken cancellationToken=default)
    {
        if(payload.Full)_rows.Clear();
        foreach(var id in payload.RemoveIds.Where(id=>!string.IsNullOrWhiteSpace(id)))_rows.Remove(id);
        foreach(var row in payload.Upserts.Where(IsValidRow))_rows[row.Id]=row;
        Cursor=string.IsNullOrWhiteSpace(payload.Cursor)?Cursor:payload.Cursor;
        await PersistAsync(cancellationToken).ConfigureAwait(false);
    }

    public IReadOnlyList<SalesOrderSkuCatalogRowData> Search(string search,int limit=30)=>SearchRows(_rows.Values,search,limit);

    public static IReadOnlyList<SalesOrderSkuCatalogRowData> SearchRows(IEnumerable<SalesOrderSkuCatalogRowData> rows,string search,int limit=30)
    {
        var raw=(search??string.Empty).Trim(); var normalized=NormalizeSearchText(raw); if(normalized.Length==0)return [];
        var tokens=normalized.Split(' ',StringSplitOptions.RemoveEmptyEntries|StringSplitOptions.TrimEntries);
        return rows.Where(IsValidRow).Where(row=>{var fields=SearchFields(row).ToArray();return tokens.All(token=>fields.Any(field=>field.Contains(token,StringComparison.Ordinal)));})
            .OrderBy(row=>SearchRank(row,raw,normalized)).ThenBy(row=>row.ProductCode,StringComparer.OrdinalIgnoreCase).ThenBy(row=>row.Sku,StringComparer.OrdinalIgnoreCase).ThenBy(row=>row.Id,StringComparer.Ordinal)
            .Take(Math.Clamp(limit,1,50)).ToArray();
    }

    public static string NormalizeSearchText(string? value)
    {
        var source=(value??string.Empty).Normalize(NormalizationForm.FormD); var builder=new StringBuilder(source.Length);
        foreach(var ch in source)if(CharUnicodeInfo.GetUnicodeCategory(ch)!=UnicodeCategory.NonSpacingMark)builder.Append(ch);
        return string.Join(" ",builder.ToString().Normalize(NormalizationForm.FormC).ToLowerInvariant().Replace('đ','d').Split((char[]?)null,StringSplitOptions.RemoveEmptyEntries|StringSplitOptions.TrimEntries));
    }

    public static int SearchRank(SalesOrderSkuCatalogRowData row,string rawTerm,string normalizedTerm)
    {
        var exact=rawTerm.Trim().ToUpperInvariant();
        if(row.Sku.Trim().ToUpperInvariant()==exact)return 0;
        if(row.ProductCode.Trim().ToUpperInvariant()==exact)return 1;
        if(AllBarcodes(row).Any(x=>x.ToUpperInvariant()==exact))return 2;
        if(NormalizeSearchText(row.ProductName)==normalizedTerm)return 3;
        if(NormalizeSearchText(row.VariantName)==normalizedTerm)return 4;
        if(NormalizeSearchText(row.ProductName).StartsWith(normalizedTerm,StringComparison.Ordinal))return 5;
        if(NormalizeSearchText(row.VariantName).StartsWith(normalizedTerm,StringComparison.Ordinal))return 6;
        return 7;
    }

    private async Task PersistAsync(CancellationToken cancellationToken)
    {
        try
        {
            var directory=Path.GetDirectoryName(_path); if(string.IsNullOrWhiteSpace(directory))return; Directory.CreateDirectory(directory);
            var temp=_path+".tmp";
            await using(var stream=File.Create(temp))await JsonSerializer.SerializeAsync(stream,new PersistedCatalog(Cursor,_rows.Values.OrderBy(x=>x.Id,StringComparer.Ordinal).ToArray()),JsonOptions,cancellationToken).ConfigureAwait(false);
            File.Move(temp,_path,true);
        }
        catch(IOException){ } catch(UnauthorizedAccessException){ }
    }

    private static bool IsValidRow(SalesOrderSkuCatalogRowData row)=>!string.IsNullOrWhiteSpace(row.Id)&&!string.IsNullOrWhiteSpace(row.ProductId)&&!string.IsNullOrWhiteSpace(row.ProductCode)&&!string.IsNullOrWhiteSpace(row.ProductName)&&!string.IsNullOrWhiteSpace(row.Sku);
    private static IEnumerable<string> SearchFields(SalesOrderSkuCatalogRowData row)=>new[]{row.Sku,row.VariantName,row.ProductCode,row.ProductName,row.Barcode}.Concat(row.Barcodes??[]).Where(x=>!string.IsNullOrWhiteSpace(x)).Select(NormalizeSearchText);
    private static IEnumerable<string> AllBarcodes(SalesOrderSkuCatalogRowData row)=>new[]{row.Barcode}.Concat(row.Barcodes??[]).Where(x=>!string.IsNullOrWhiteSpace(x)).Select(x=>x!);
}
