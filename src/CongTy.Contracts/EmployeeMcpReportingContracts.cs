namespace CongTy.Contracts;

public sealed record EmployeeMcpFiltersData
{
    public string From { get; init; } = string.Empty;
    public string To { get; init; } = string.Empty;
}

public sealed record EmployeeMcpScopeData
{
    public string Basis { get; init; } = string.Empty;
    public string? EmployeeId { get; init; }
    public string? EmployeeCode { get; init; }
}

public sealed record EmployeeMcpBasisData
{
    public string Identity { get; init; } = string.Empty;
    public string Territory { get; init; } = string.Empty;
    public string Visits { get; init; } = string.Empty;
    public string Conversion { get; init; } = string.Empty;
    public string CustomerBoundary { get; init; } = string.Empty;
    public string AdminReuse { get; init; } = string.Empty;
}

public sealed record EmployeeMcpSummaryData
{
    public string? SessionCount { get; init; }
    public string? RouteCount { get; init; }
    public string? PlannedOutletCount { get; init; }
    public string? PlannedVisitedOutletCount { get; init; }
    public string? VisitedOutletCount { get; init; }
    public string? CheckedInOutletCount { get; init; }
    public string? VisitCount { get; init; }
    public string? OrderIntentCount { get; init; }
    public string? OnboardingSubmittedCount { get; init; }
    public string? OnboardingConvertedCount { get; init; }
    public string? CoreSalesOrderCount { get; init; }
    public string? MappedEmployeeSessionCount { get; init; }
    public string? UnmappedEmployeeSessionCount { get; init; }
    public string? CounterMismatchSessionCount { get; init; }
    public string? PlannedVisitRatePercent { get; init; }
    public string? OrderIntentConversionPercent { get; init; }
    public string? OnboardingConversionPercent { get; init; }
    public string? CoreOrderConversionPercent { get; init; }
}

public sealed record EmployeeMcpActorRowData
{
    public string? SalesLabel { get; init; }
    public string? EmployeeId { get; init; }
    public string? EmployeeCode { get; init; }
    public string? EmployeeName { get; init; }
    public string SessionCount { get; init; } = "0";
    public string RouteCount { get; init; } = "0";
    public string PlannedOutletCount { get; init; } = "0";
    public string PlannedVisitedOutletCount { get; init; } = "0";
    public string VisitedOutletCount { get; init; } = "0";
    public string CheckedInOutletCount { get; init; } = "0";
    public string VisitCount { get; init; } = "0";
    public string OrderIntentCount { get; init; } = "0";
    public string OnboardingSubmittedCount { get; init; } = "0";
    public string OnboardingConvertedCount { get; init; } = "0";
    public string CoreSalesOrderCount { get; init; } = "0";
    public string? PlannedVisitRatePercent { get; init; }
    public string? OrderIntentConversionPercent { get; init; }
    public string? CoreOrderConversionPercent { get; init; }
}

public sealed record EmployeeMcpRouteRowData
{
    public string RouteId { get; init; } = string.Empty;
    public string? RouteCode { get; init; }
    public string RouteName { get; init; } = string.Empty;
    public string? Area { get; init; }
    public string? SalesLabel { get; init; }
    public string? EmployeeId { get; init; }
    public string? EmployeeCode { get; init; }
    public string? EmployeeName { get; init; }
    public string SessionCount { get; init; } = "0";
    public string PlannedOutletCount { get; init; } = "0";
    public string PlannedVisitedOutletCount { get; init; } = "0";
    public string VisitedOutletCount { get; init; } = "0";
    public string CheckedInOutletCount { get; init; } = "0";
    public string OrderIntentCount { get; init; } = "0";
    public string CoreSalesOrderCount { get; init; } = "0";
    public string? PlannedVisitRatePercent { get; init; }
}

public sealed record EmployeeMcpSessionRowData
{
    public string SessionId { get; init; } = string.Empty;
    public string SessionDate { get; init; } = string.Empty;
    public string RouteId { get; init; } = string.Empty;
    public string? RouteCode { get; init; }
    public string RouteName { get; init; } = string.Empty;
    public string? Area { get; init; }
    public string? SalesLabel { get; init; }
    public string? EmployeeId { get; init; }
    public string? EmployeeCode { get; init; }
    public string? EmployeeName { get; init; }
    public string Status { get; init; } = string.Empty;
    public string PlannedOutletCount { get; init; } = "0";
    public string PlannedVisitedOutletCount { get; init; } = "0";
    public string VisitedOutletCount { get; init; } = "0";
    public string CheckedInOutletCount { get; init; } = "0";
    public string VisitCount { get; init; } = "0";
    public string OrderIntentCount { get; init; } = "0";
    public string OnboardingSubmittedCount { get; init; } = "0";
    public string OnboardingConvertedCount { get; init; } = "0";
    public string CoreSalesOrderCount { get; init; } = "0";
    public bool StoredCounterMismatch { get; init; }
    public string? OpenedAt { get; init; }
    public string? ClosedAt { get; init; }
}

public sealed record EmployeeMcpUnmappedActorData
{
    public string ExceptionCode { get; init; } = string.Empty;
    public string? SalesLabel { get; init; }
    public string SessionCount { get; init; } = "0";
    public string FirstSessionDate { get; init; } = string.Empty;
    public string LastSessionDate { get; init; } = string.Empty;
}

public sealed record EmployeeMcpCounterMismatchData
{
    public string ExceptionCode { get; init; } = string.Empty;
    public string SessionId { get; init; } = string.Empty;
    public string SessionDate { get; init; } = string.Empty;
    public string RouteId { get; init; } = string.Empty;
    public string? RouteCode { get; init; }
    public string RouteName { get; init; } = string.Empty;
    public string? SalesLabel { get; init; }
    public string StoredPlannedCustomers { get; init; } = "0";
    public string DerivedPlannedOutletCount { get; init; } = "0";
    public string StoredVisitedCustomers { get; init; } = "0";
    public string DerivedVisitedOutletCount { get; init; } = "0";
    public string StoredOrderCount { get; init; } = "0";
    public string DerivedOrderIntentCount { get; init; } = "0";
}

public sealed record EmployeeMcpDataQualityData
{
    public EmployeeMcpUnmappedActorData[] UnmappedActors { get; init; } = [];
    public EmployeeMcpCounterMismatchData[] CounterMismatches { get; init; } = [];
}

public sealed record EmployeeMcpDashboardData
{
    public string Family { get; init; } = string.Empty;
    public string GeneratedAt { get; init; } = string.Empty;
    public string Timezone { get; init; } = string.Empty;
    public EmployeeMcpFiltersData Filters { get; init; } = new();
    public EmployeeMcpScopeData Scope { get; init; } = new();
    public EmployeeMcpBasisData Basis { get; init; } = new();
    public EmployeeMcpSummaryData Summary { get; init; } = new();
    public EmployeeMcpActorRowData[] FieldActors { get; init; } = [];
    public EmployeeMcpRouteRowData[] Routes { get; init; } = [];
    public EmployeeMcpSessionRowData[] Sessions { get; init; } = [];
    public EmployeeMcpDataQualityData DataQuality { get; init; } = new();
}
