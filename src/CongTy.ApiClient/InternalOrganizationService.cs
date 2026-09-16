using CongTy.Contracts;

namespace CongTy.ApiClient;

public interface IInternalOrganizationService
{
    Task<IReadOnlyList<BranchData>> ListBranchesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<WarehouseData>> ListWarehousesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<WarehouseLocationData>> ListLocationsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<EmployeeData>> ListEmployeesAsync(CancellationToken cancellationToken = default);
    Task<InternalOrganizationSnapshot> LoadAsync(CancellationToken cancellationToken = default);

    Task<BranchData> CreateBranchAsync(BranchCreateRequest request, string idempotencyKey, CancellationToken cancellationToken = default);
    Task<BranchData> UpdateBranchAsync(string id, BranchUpdateRequest request, CancellationToken cancellationToken = default);
    Task<BranchData> SetBranchActiveAsync(string id, bool isActive, string expectedUpdatedAt, CancellationToken cancellationToken = default);

    Task<WarehouseData> CreateWarehouseAsync(WarehouseCreateRequest request, string idempotencyKey, CancellationToken cancellationToken = default);
    Task<WarehouseData> UpdateWarehouseAsync(string id, WarehouseUpdateRequest request, CancellationToken cancellationToken = default);
    Task<WarehouseData> SetWarehouseActiveAsync(string id, bool isActive, string expectedUpdatedAt, CancellationToken cancellationToken = default);

    Task<WarehouseLocationData> CreateLocationAsync(WarehouseLocationCreateRequest request, string idempotencyKey, CancellationToken cancellationToken = default);
    Task<WarehouseLocationData> UpdateLocationAsync(string id, WarehouseLocationUpdateRequest request, CancellationToken cancellationToken = default);
    Task<WarehouseLocationData> SetLocationActiveAsync(string id, bool isActive, string expectedUpdatedAt, CancellationToken cancellationToken = default);

    Task<EmployeeData> CreateEmployeeAsync(EmployeeCreateRequest request, string idempotencyKey, CancellationToken cancellationToken = default);
    Task<EmployeeData> UpdateEmployeeAsync(string id, EmployeeUpdateRequest request, CancellationToken cancellationToken = default);
    Task<EmployeeData> SetEmployeeActiveAsync(string id, bool isActive, string expectedUpdatedAt, CancellationToken cancellationToken = default);

    Task<WarehouseLocationModePreviewData> PreviewLocationModeAsync(
        string warehouseId,
        string targetMode,
        string? destinationLocationId,
        CancellationToken cancellationToken = default);

    Task<WarehouseLocationModeRun> ConvertLocationModeAsync(
        string warehouseId,
        WarehouseLocationModeConvertRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<WarehouseLocationModeRun>> ListLocationModeRunsAsync(
        string warehouseId,
        CancellationToken cancellationToken = default);

    Task<WarehouseLocationModeRun> GetLocationModeRunAsync(
        string runId,
        CancellationToken cancellationToken = default);
}

public sealed class InternalOrganizationService(
    CompanyApiClient apiClient,
    IAuthenticatedSessionAccessor sessionAccessor,
    ICanonicalIdempotencyKeyProvider idempotencyKeys) : IInternalOrganizationService
{
    public async Task<IReadOnlyList<BranchData>> ListBranchesAsync(CancellationToken cancellationToken = default) =>
        await apiClient.GetDataAsync<BranchData[]>(
            "/api/branches?limit=1000&offset=0",
            RequireToken(),
            cancellationToken).ConfigureAwait(false);

    public async Task<IReadOnlyList<WarehouseData>> ListWarehousesAsync(CancellationToken cancellationToken = default) =>
        await apiClient.GetDataAsync<WarehouseData[]>(
            "/api/warehouses?limit=1000&offset=0",
            RequireToken(),
            cancellationToken).ConfigureAwait(false);

    public async Task<IReadOnlyList<WarehouseLocationData>> ListLocationsAsync(CancellationToken cancellationToken = default) =>
        await apiClient.GetDataAsync<WarehouseLocationData[]>(
            "/api/warehouse-locations?limit=1000&offset=0",
            RequireToken(),
            cancellationToken).ConfigureAwait(false);

    public async Task<IReadOnlyList<EmployeeData>> ListEmployeesAsync(CancellationToken cancellationToken = default) =>
        await apiClient.GetDataAsync<EmployeeData[]>(
            "/api/employees?limit=1000&offset=0",
            RequireToken(),
            cancellationToken).ConfigureAwait(false);

    public async Task<InternalOrganizationSnapshot> LoadAsync(CancellationToken cancellationToken = default)
    {
        var branchesTask = ListBranchesAsync(cancellationToken);
        var warehousesTask = ListWarehousesAsync(cancellationToken);
        var locationsTask = ListLocationsAsync(cancellationToken);
        var employeesTask = ListEmployeesAsync(cancellationToken);

        await Task.WhenAll(branchesTask, warehousesTask, locationsTask, employeesTask).ConfigureAwait(false);

        return new InternalOrganizationSnapshot(
            await branchesTask.ConfigureAwait(false),
            await warehousesTask.ConfigureAwait(false),
            await locationsTask.ConfigureAwait(false),
            await employeesTask.ConfigureAwait(false));
    }

    public Task<BranchData> CreateBranchAsync(
        BranchCreateRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken = default) =>
        PostCreateAsync<BranchCreateRequest, BranchData>("/api/branches", request, idempotencyKey, cancellationToken);

    public Task<BranchData> UpdateBranchAsync(
        string id,
        BranchUpdateRequest request,
        CancellationToken cancellationToken = default) =>
        PatchAsync<BranchUpdateRequest, BranchData>($"/api/branches/{RequireId(id)}", request, cancellationToken);

    public Task<BranchData> SetBranchActiveAsync(
        string id,
        bool isActive,
        string expectedUpdatedAt,
        CancellationToken cancellationToken = default) =>
        PatchAsync<ActiveStatusUpdateRequest, BranchData>(
            $"/api/branches/{RequireId(id)}",
            new ActiveStatusUpdateRequest(isActive, expectedUpdatedAt),
            cancellationToken);

    public Task<WarehouseData> CreateWarehouseAsync(
        WarehouseCreateRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken = default) =>
        PostCreateAsync<WarehouseCreateRequest, WarehouseData>("/api/warehouses", request, idempotencyKey, cancellationToken);

    public Task<WarehouseData> UpdateWarehouseAsync(
        string id,
        WarehouseUpdateRequest request,
        CancellationToken cancellationToken = default) =>
        PatchAsync<WarehouseUpdateRequest, WarehouseData>($"/api/warehouses/{RequireId(id)}", request, cancellationToken);

    public Task<WarehouseData> SetWarehouseActiveAsync(
        string id,
        bool isActive,
        string expectedUpdatedAt,
        CancellationToken cancellationToken = default) =>
        PatchAsync<ActiveStatusUpdateRequest, WarehouseData>(
            $"/api/warehouses/{RequireId(id)}",
            new ActiveStatusUpdateRequest(isActive, expectedUpdatedAt),
            cancellationToken);

    public Task<WarehouseLocationData> CreateLocationAsync(
        WarehouseLocationCreateRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken = default) =>
        PostCreateAsync<WarehouseLocationCreateRequest, WarehouseLocationData>(
            "/api/warehouse-locations",
            request,
            idempotencyKey,
            cancellationToken);

    public Task<WarehouseLocationData> UpdateLocationAsync(
        string id,
        WarehouseLocationUpdateRequest request,
        CancellationToken cancellationToken = default) =>
        PatchAsync<WarehouseLocationUpdateRequest, WarehouseLocationData>(
            $"/api/warehouse-locations/{RequireId(id)}",
            request,
            cancellationToken);

    public Task<WarehouseLocationData> SetLocationActiveAsync(
        string id,
        bool isActive,
        string expectedUpdatedAt,
        CancellationToken cancellationToken = default) =>
        PatchAsync<ActiveStatusUpdateRequest, WarehouseLocationData>(
            $"/api/warehouse-locations/{RequireId(id)}",
            new ActiveStatusUpdateRequest(isActive, expectedUpdatedAt),
            cancellationToken);

    public Task<EmployeeData> CreateEmployeeAsync(
        EmployeeCreateRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken = default) =>
        PostCreateAsync<EmployeeCreateRequest, EmployeeData>("/api/employees", request, idempotencyKey, cancellationToken);

    public Task<EmployeeData> UpdateEmployeeAsync(
        string id,
        EmployeeUpdateRequest request,
        CancellationToken cancellationToken = default) =>
        PatchAsync<EmployeeUpdateRequest, EmployeeData>($"/api/employees/{RequireId(id)}", request, cancellationToken);

    public Task<EmployeeData> SetEmployeeActiveAsync(
        string id,
        bool isActive,
        string expectedUpdatedAt,
        CancellationToken cancellationToken = default) =>
        PatchAsync<ActiveStatusUpdateRequest, EmployeeData>(
            $"/api/employees/{RequireId(id)}",
            new ActiveStatusUpdateRequest(isActive, expectedUpdatedAt),
            cancellationToken);

    public Task<WarehouseLocationModePreviewData> PreviewLocationModeAsync(
        string warehouseId,
        string targetMode,
        string? destinationLocationId,
        CancellationToken cancellationToken = default)
    {
        var path =
            $"/api/inventory/warehouses/{RequireId(warehouseId)}/location-mode/preview?targetMode={Uri.EscapeDataString(RequireMode(targetMode))}";

        if (!string.IsNullOrWhiteSpace(destinationLocationId))
        {
            path += $"&destinationLocationId={Uri.EscapeDataString(RequireId(destinationLocationId))}";
        }

        return apiClient.GetDataAsync<WarehouseLocationModePreviewData>(
            path,
            RequireToken(),
            cancellationToken);
    }

    public Task<WarehouseLocationModeRun> ConvertLocationModeAsync(
        string warehouseId,
        WarehouseLocationModeConvertRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        RequireMode(request.TargetMode);
        if (request.PreviewHash.Length != 64 || request.PreviewHash.Any(character => !Uri.IsHexDigit(character)))
        {
            throw new ArgumentException("Bản xem trước thay đổi sơ đồ kho không hợp lệ.", nameof(request));
        }

        return PostCreateAsync<WarehouseLocationModeConvertRequest, WarehouseLocationModeRun>(
            $"/api/inventory/warehouses/{RequireId(warehouseId)}/location-mode/convert",
            request,
            idempotencyKey,
            cancellationToken);
    }

    public async Task<IReadOnlyList<WarehouseLocationModeRun>> ListLocationModeRunsAsync(
        string warehouseId,
        CancellationToken cancellationToken = default) =>
        await apiClient.GetDataAsync<WarehouseLocationModeRun[]>(
            $"/api/inventory/warehouses/{RequireId(warehouseId)}/location-mode/runs?limit=100&offset=0",
            RequireToken(),
            cancellationToken).ConfigureAwait(false);

    public Task<WarehouseLocationModeRun> GetLocationModeRunAsync(
        string runId,
        CancellationToken cancellationToken = default) =>
        apiClient.GetDataAsync<WarehouseLocationModeRun>(
            $"/api/inventory/location-mode-runs/{RequireId(runId)}",
            RequireToken(),
            cancellationToken);

    private Task<TResponse> PostCreateAsync<TRequest, TResponse>(
        string path,
        TRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken)
    {
        if (!idempotencyKeys.IsValid(idempotencyKey))
        {
            throw new ArgumentException("Khóa chống xử lý trùng không hợp lệ.", nameof(idempotencyKey));
        }

        return apiClient.PostIdempotentDataAsync<TRequest, TResponse>(
            path,
            request,
            idempotencyKey,
            RequireToken(),
            cancellationToken);
    }

    private Task<TResponse> PatchAsync<TRequest, TResponse>(
        string path,
        TRequest request,
        CancellationToken cancellationToken) =>
        apiClient.PatchDataAsync<TRequest, TResponse>(
            path,
            request,
            RequireToken(),
            cancellationToken);

    private string RequireToken() =>
        string.IsNullOrWhiteSpace(sessionAccessor.CurrentToken)
            ? throw new InvalidOperationException("Phiên đăng nhập không còn hiệu lực.")
            : sessionAccessor.CurrentToken;

    private static string RequireId(string value) =>
        Guid.TryParse(value, out _)
            ? value.Trim()
            : throw new ArgumentException("Mã dữ liệu không hợp lệ.", nameof(value));

    private static string RequireMode(string value) =>
        value is "MANAGED" or "UNMANAGED"
            ? value
            : throw new ArgumentException("Chế độ quản lý vị trí kho không hợp lệ.", nameof(value));
}
