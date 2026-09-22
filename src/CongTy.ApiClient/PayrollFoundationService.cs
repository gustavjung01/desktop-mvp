using CongTy.Contracts;

namespace CongTy.ApiClient;

public interface IPayrollFoundationService
{
    Task<PayrollFoundationData> GetAsync(string? payrollPeriodId = null, CancellationToken cancellationToken = default);
    Task<PayrollPeriodData> CreatePeriodAsync(CreatePayrollPeriodRequest request, string idempotencyKey, CancellationToken cancellationToken = default);
    Task<PayrollSalaryProfileData> SaveSalaryAsync(SavePayrollSalaryRequest request, string idempotencyKey, CancellationToken cancellationToken = default);
    Task<PayrollComponentTypeData> CreateComponentTypeAsync(CreatePayrollComponentTypeRequest request, string idempotencyKey, CancellationToken cancellationToken = default);
    Task<PayrollFixedComponentData> AssignFixedComponentAsync(AssignPayrollFixedComponentRequest request, string idempotencyKey, CancellationToken cancellationToken = default);
    Task<PayrollPeriodComponentData> AddPeriodComponentAsync(AddPayrollPeriodComponentRequest request, string idempotencyKey, CancellationToken cancellationToken = default);
    Task<PayrollAggregationMutationData> AggregateAsync(AggregatePayrollRequest request, string idempotencyKey, CancellationToken cancellationToken = default);
    Task<PayrollAggregationMutationData> ReconcileAsync(ReconcilePayrollRequest request, string idempotencyKey, CancellationToken cancellationToken = default);
    Task<ClosePayrollMutationData> CloseAsync(ClosePayrollRequest request, string idempotencyKey, CancellationToken cancellationToken = default);
    Task<AdjustPayrollMutationData> AdjustAsync(AdjustPayrollRequest request, string idempotencyKey, CancellationToken cancellationToken = default);
}

public sealed class PayrollFoundationService(
    CompanyApiClient apiClient,
    IAuthenticatedSessionAccessor sessionAccessor) : IPayrollFoundationService
{
    public Task<PayrollFoundationData> GetAsync(
        string? payrollPeriodId = null,
        CancellationToken cancellationToken = default)
    {
        var path = "/api/workforce/payroll";
        var normalized = payrollPeriodId?.Trim();
        if (!string.IsNullOrWhiteSpace(normalized))
            path += "?periodId=" + Uri.EscapeDataString(normalized);

        return apiClient.GetDataAsync<PayrollFoundationData>(
            path,
            RequireToken(),
            cancellationToken);
    }

    public Task<PayrollPeriodData> CreatePeriodAsync(
        CreatePayrollPeriodRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken = default) =>
        PostAsync<CreatePayrollPeriodRequest, PayrollPeriodData>(request, idempotencyKey, cancellationToken);

    public Task<PayrollSalaryProfileData> SaveSalaryAsync(
        SavePayrollSalaryRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken = default) =>
        PostAsync<SavePayrollSalaryRequest, PayrollSalaryProfileData>(request, idempotencyKey, cancellationToken);

    public Task<PayrollComponentTypeData> CreateComponentTypeAsync(
        CreatePayrollComponentTypeRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken = default) =>
        PostAsync<CreatePayrollComponentTypeRequest, PayrollComponentTypeData>(request, idempotencyKey, cancellationToken);

    public Task<PayrollFixedComponentData> AssignFixedComponentAsync(
        AssignPayrollFixedComponentRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken = default) =>
        PostAsync<AssignPayrollFixedComponentRequest, PayrollFixedComponentData>(request, idempotencyKey, cancellationToken);

    public Task<PayrollPeriodComponentData> AddPeriodComponentAsync(
        AddPayrollPeriodComponentRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken = default) =>
        PostAsync<AddPayrollPeriodComponentRequest, PayrollPeriodComponentData>(request, idempotencyKey, cancellationToken);

    public Task<PayrollAggregationMutationData> AggregateAsync(
        AggregatePayrollRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken = default) =>
        PostAsync<AggregatePayrollRequest, PayrollAggregationMutationData>(request, idempotencyKey, cancellationToken);

    public Task<PayrollAggregationMutationData> ReconcileAsync(
        ReconcilePayrollRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken = default) =>
        PostAsync<ReconcilePayrollRequest, PayrollAggregationMutationData>(request, idempotencyKey, cancellationToken);

    public Task<ClosePayrollMutationData> CloseAsync(
        ClosePayrollRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken = default) =>
        PostAsync<ClosePayrollRequest, ClosePayrollMutationData>(request, idempotencyKey, cancellationToken);

    public Task<AdjustPayrollMutationData> AdjustAsync(
        AdjustPayrollRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken = default) =>
        PostAsync<AdjustPayrollRequest, AdjustPayrollMutationData>(request, idempotencyKey, cancellationToken);

    private Task<TResponse> PostAsync<TRequest, TResponse>(
        TRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken) =>
        apiClient.PostIdempotentDataAsync<TRequest, TResponse>(
            "/api/workforce/payroll",
            request,
            idempotencyKey,
            RequireToken(),
            cancellationToken);

    private string RequireToken() =>
        string.IsNullOrWhiteSpace(sessionAccessor.CurrentToken)
            ? throw new InvalidOperationException("Phiên đăng nhập không còn hiệu lực.")
            : sessionAccessor.CurrentToken;
}
