using CongTy.ApiClient;
using CongTy.Contracts;

namespace CongTy.Desktop.Sales;

internal sealed class OrderManagementQueryService(
    CompanyApiClient apiClient,
    IAuthenticatedSessionAccessor sessionAccessor)
{
    public async Task<IReadOnlyList<SalesOrderData>> ListAllAsync(CancellationToken cancellationToken=default)
    {
        const int pageSize=1000;
        var offset=0;
        var result=new List<SalesOrderData>();
        while(true)
        {
            var page=await apiClient.GetDataAsync<SalesOrderData[]>(
                $"/api/sales-orders?limit={pageSize}&offset={offset}",
                RequireToken(),cancellationToken).ConfigureAwait(false);
            result.AddRange(page);
            if(page.Length<pageSize)break;
            offset+=page.Length;
            if(offset>=100000)break;
        }
        return result;
    }

    public Task<SalesOrderData> GetAsync(string id,CancellationToken cancellationToken=default)
    {
        if(!Guid.TryParse(id,out _))throw new ArgumentException("Mã đơn bán hàng không hợp lệ.",nameof(id));
        return apiClient.GetDataAsync<SalesOrderData>($"/api/sales-orders/{id.Trim()}",RequireToken(),cancellationToken);
    }

    private string RequireToken()
    {
        var token=sessionAccessor.CurrentToken;
        return string.IsNullOrWhiteSpace(token)
            ? throw new InvalidOperationException("Phiên đăng nhập không còn hiệu lực.")
            : token;
    }
}
