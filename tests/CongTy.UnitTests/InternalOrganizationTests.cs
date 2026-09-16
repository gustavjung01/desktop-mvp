using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using CongTy.ApiClient;
using CongTy.Contracts;
using CongTy.Desktop.Organization;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CongTy.UnitTests;

[TestClass]
public sealed class InternalOrganizationTests
{
    private const string Token = "nppusr.11111111-1111-4111-8111-111111111111.abcdefghijklmnopqrstuvwxyzABCDE1234567890";

    [TestMethod]
    public void CanonicalIdempotencyKey_UsesOnlyContractCharacters()
    {
        var provider = new CanonicalIdempotencyKeyProvider();

        var first = provider.Create("organization create");
        var second = provider.Create("organization create");

        Assert.IsTrue(provider.IsValid(first));
        Assert.IsTrue(provider.IsValid(second));
        Assert.AreNotEqual(first, second);
        Assert.IsLessThanOrEqualTo(128, first.Length);
        Assert.IsFalse(first.Any(character =>
            !(char.IsAsciiLetterOrDigit(character) || character is '.' or '_' or '-')));
    }

    [TestMethod]
    public async Task CreateBranch_UsesDirectBackendAndReusesCallerKey()
    {
        var seen = new List<(string Path, AuthenticationHeaderValue? Authorization, string? IdempotencyKey)>();
        var handler = new DelegateHandler(request =>
        {
            seen.Add((
                request.RequestUri!.AbsolutePath,
                request.Headers.Authorization,
                request.Headers.TryGetValues("Idempotency-Key", out var keys) ? keys.Single() : null));

            return Json(HttpStatusCode.Created, """
            {
              "data": {
                "id": "11111111-1111-4111-8111-111111111111",
                "code": "CN01",
                "name": "Chi nhánh 01",
                "address": null,
                "phone": null,
                "email": null,
                "is_active": true,
                "created_at": "2026-09-14T00:00:00.000Z",
                "updated_at": "2026-09-14T00:00:00.000Z"
              },
              "requestId": "req_branch",
              "receivedAt": "2026-09-14T00:00:00.000Z"
            }
            """);
        });

        var keyProvider = new CanonicalIdempotencyKeyProvider();
        var key = keyProvider.Create("organization-create");
        var service = CreateService(handler, keyProvider);

        var request = new BranchCreateRequest("CN01", "Chi nhánh 01", null, null, null);
        await service.CreateBranchAsync(request, key);
        await service.CreateBranchAsync(request, key);

        Assert.HasCount(2, seen);
        Assert.AreEqual("/api/branches", seen[0].Path);
        var authorization = seen[0].Authorization;
        Assert.IsNotNull(authorization);
        Assert.AreEqual("Bearer", authorization.Scheme);
        Assert.AreEqual(Token, authorization.Parameter);
        Assert.AreEqual(key, seen[0].IdempotencyKey);
        Assert.AreEqual(key, seen[1].IdempotencyKey);
    }

    [TestMethod]
    public async Task UpdateEmployee_SendsExpectedUpdatedAtWithoutIdempotencyHeader()
    {
        HttpRequestMessage? captured = null;
        string? body = null;

        var handler = new DelegateHandler(async request =>
        {
            captured = request;
            body = request.Content is null ? null : await request.Content.ReadAsStringAsync();
            return Json(HttpStatusCode.OK, """
            {
              "data": {
                "id": "22222222-2222-4222-8222-222222222222",
                "code": "NV01",
                "full_name": "Nguyễn Văn A",
                "job_title": "Kế toán",
                "phone": null,
                "email": null,
                "branch_id": null,
                "is_active": true,
                "created_at": "2026-09-14T00:00:00.000Z",
                "updated_at": "2026-09-14T01:00:00.000Z"
              },
              "requestId": "req_employee",
              "receivedAt": "2026-09-14T01:00:00.000Z"
            }
            """);
        });

        var service = CreateService(handler, new CanonicalIdempotencyKeyProvider());
        const string expectedUpdatedAt = "2026-09-14T00:00:00.000Z";

        await service.UpdateEmployeeAsync(
            "22222222-2222-4222-8222-222222222222",
            new EmployeeUpdateRequest(
                "Nguyễn Văn A",
                "Kế toán",
                null,
                null,
                null,
                expectedUpdatedAt));

        Assert.IsNotNull(captured);
        Assert.AreEqual(HttpMethod.Patch, captured.Method);
        Assert.AreEqual(
            "/api/employees/22222222-2222-4222-8222-222222222222",
            captured.RequestUri!.AbsolutePath);
        Assert.IsFalse(captured.Headers.Contains("Idempotency-Key"));
        Assert.IsNotNull(body);

        using var document = JsonDocument.Parse(body);
        Assert.AreEqual(expectedUpdatedAt, document.RootElement.GetProperty("expectedUpdatedAt").GetString());
    }

    [TestMethod]
    public async Task LoadOrganization_UsesCanonicalDirectEndpoints()
    {
        var paths = new List<string>();
        var handler = new DelegateHandler(request =>
        {
            paths.Add(request.RequestUri!.PathAndQuery);
            return Json(HttpStatusCode.OK, $$"""
            {
              "data": [],
              "requestId": "req_{{paths.Count}}",
              "receivedAt": "2026-09-14T00:00:00.000Z"
            }
            """);
        });

        var service = CreateService(handler, new CanonicalIdempotencyKeyProvider());
        var snapshot = await service.LoadAsync();

        Assert.HasCount(0, snapshot.Branches);
        Assert.HasCount(0, snapshot.Warehouses);
        Assert.HasCount(0, snapshot.Locations);
        Assert.HasCount(0, snapshot.Employees);
        CollectionAssert.AreEquivalent(
            new[]
            {
                "/api/branches?limit=1000&offset=0",
                "/api/warehouses?limit=1000&offset=0",
                "/api/warehouse-locations?limit=1000&offset=0",
                "/api/employees?limit=1000&offset=0"
            },
            paths);
    }

    [TestMethod]
    public void OrganizationNavigation_IsPermissionDrivenAndDenyByDefault()
    {
        var access = new AccessStateService();

        Assert.IsTrue(access.TryApply(
            CreateMe(["core.branch.read"]),
            out var error));
        Assert.AreEqual(string.Empty, error);
        Assert.IsTrue(access.CanNavigate("internal-organization"));
        Assert.IsFalse(access.CanNavigate("sales"));

        access.Clear();

        Assert.IsTrue(access.TryApply(
            CreateMe(["core.customer.read"]),
            out error));
        Assert.AreEqual(string.Empty, error);
        Assert.IsFalse(access.CanNavigate("internal-organization"));
    }

    [TestMethod]
    public void OrganizationOverview_BuildsWebHierarchyAndEightNewestChanges()
    {
        var branches = new[]
        {
            new BranchData
            {
                Id = "11111111-1111-4111-8111-111111111111",
                Code = "CN01",
                Name = "Chi nhánh 01",
                Address = "Hà Nội",
                IsActive = true,
                UpdatedAt = "2026-09-15T01:00:00Z"
            }
        };
        var warehouses = new[]
        {
            new WarehouseData
            {
                Id = "22222222-2222-4222-8222-222222222222",
                BranchId = branches[0].Id,
                Code = "K01",
                Name = "Kho 01",
                WarehouseType = "main",
                IsActive = true,
                UpdatedAt = "2026-09-15T02:00:00Z"
            }
        };
        var locations = Enumerable.Range(1, 9)
            .Select(index => new WarehouseLocationData
            {
                Id = $"33333333-3333-4333-8333-{index:D12}",
                WarehouseId = warehouses[0].Id,
                Code = $"VT{index:D2}",
                Name = $"Vị trí {index:D2}",
                LocationType = "storage",
                IsActive = index != 9,
                UpdatedAt = $"2026-09-15T{index + 2:D2}:00:00Z"
            })
            .ToArray();

        var hierarchy = OrganizationPresentation.BuildOverviewHierarchy(branches, warehouses, locations);
        var recent = OrganizationPresentation.BuildOverviewRecent(branches, warehouses, locations);

        Assert.HasCount(1, hierarchy);
        Assert.AreEqual(1, hierarchy[0].WarehouseCount);
        Assert.AreEqual(1, hierarchy[0].ActiveWarehouseCount);
        Assert.AreEqual(9, hierarchy[0].LocationCount);
        Assert.AreEqual("Hà Nội", hierarchy[0].Address);

        Assert.HasCount(8, recent);
        Assert.AreEqual("VT09", recent[0].Code);
        Assert.AreEqual("Ngừng hoạt động", recent[0].Status);
        StringAssert.Contains(recent[0].Relation, "CN01");
        StringAssert.Contains(recent[0].Relation, "K01");
        StringAssert.Contains(recent[0].Relation, "Khu lưu trữ");
    }

    [TestMethod]
    public void BranchPresentation_UsesCurrentWebLabels()
    {
        Assert.AreEqual(
            "Tất cả",
            OrganizationPresentation.BranchStatusOptions.Single(option => option.Id == "all").Label);
        Assert.AreEqual("Ngừng sử dụng", OrganizationPresentation.BranchStatusAction(true));
        Assert.AreEqual("Đưa vào sử dụng", OrganizationPresentation.BranchStatusAction(false));
    }

    [TestMethod]
    public void WarehousePresentation_UsesCurrentWebLabels()
    {
        Assert.AreEqual(
            "Tất cả",
            OrganizationPresentation.WarehouseStatusOptions.Single(option => option.Id == "all").Label);
        Assert.AreEqual("Đang bật", OrganizationPresentation.WarehouseNegativeStockStatus(true));
        Assert.AreEqual("Đang tắt", OrganizationPresentation.WarehouseNegativeStockStatus(false));
        Assert.AreEqual("Ngừng sử dụng", OrganizationPresentation.StatusAction(true));
        Assert.AreEqual("Đưa vào sử dụng", OrganizationPresentation.StatusAction(false));
        Assert.AreEqual(
            "Khu vực khác",
            OrganizationPresentation.WarehouseLocationTypeOptions.Single(option => option.Id == "other").Label);
        Assert.AreEqual("Khu vực khác", OrganizationPresentation.WarehouseLocationType("other"));
    }

    [TestMethod]
    public async Task WarehouseLocationMode_UsesInventoryBackendAndCanonicalKey()
    {
        var seen = new List<(string Method, string PathAndQuery, string? IdempotencyKey)>();
        var handler = new DelegateHandler(request =>
        {
            seen.Add((
                request.Method.Method,
                request.RequestUri!.PathAndQuery,
                request.Headers.TryGetValues("Idempotency-Key", out var keys) ? keys.Single() : null));

            if (request.Method == HttpMethod.Get)
            {
                return Json(HttpStatusCode.OK, """
                {
                  "data": {
                    "targetMode": "UNMANAGED",
                    "destinationLocation": null,
                    "summary": {
                      "affectedSkuCount": 2,
                      "affectedScopeCount": 3,
                      "totalBaseQuantity": "10",
                      "relocatedReservationCount": 0
                    },
                    "blockers": [],
                    "canConvert": true,
                    "previewHash": "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"
                  },
                  "requestId": "req_preview",
                  "receivedAt": "2026-09-14T00:00:00.000Z"
                }
                """);
            }

            return Json(HttpStatusCode.OK, """
            {
              "data": {
                "id": "33333333-3333-4333-8333-333333333333",
                "warehouseId": "44444444-4444-4444-8444-444444444444",
                "warehouseCode": "K01",
                "warehouseName": "Kho 01",
                "fromMode": "MANAGED",
                "targetMode": "UNMANAGED",
                "destinationLocationCode": null,
                "destinationLocationName": null,
                "affectedSkuCount": 2,
                "affectedScopeCount": 3,
                "totalBaseQuantity": "10",
                "completedAt": "2026-09-14T00:00:00.000Z",
                "completedBy": "user:test"
              },
              "requestId": "req_convert",
              "receivedAt": "2026-09-14T00:00:00.000Z"
            }
            """);
        });

        var keys = new CanonicalIdempotencyKeyProvider();
        var service = CreateService(handler, keys);
        const string warehouseId = "44444444-4444-4444-8444-444444444444";

        var preview = await service.PreviewLocationModeAsync(
            warehouseId,
            "UNMANAGED",
            null);
        var key = keys.Create("warehouse-location-mode-convert");
        await service.ConvertLocationModeAsync(
            warehouseId,
            new WarehouseLocationModeConvertRequest(
                "UNMANAGED",
                null,
                preview.PreviewHash),
            key);

        Assert.HasCount(2, seen);
        Assert.AreEqual(
            $"/api/inventory/warehouses/{warehouseId}/location-mode/preview?targetMode=UNMANAGED",
            seen[0].PathAndQuery);
        Assert.AreEqual("POST", seen[1].Method);
        Assert.AreEqual(
            $"/api/inventory/warehouses/{warehouseId}/location-mode/convert",
            seen[1].PathAndQuery);
        Assert.AreEqual(key, seen[1].IdempotencyKey);
    }

    [TestMethod]
    public void WarehouseLocationReadPermission_EnablesOrganizationNavigation()
    {
        var access = new AccessStateService();

        Assert.IsTrue(access.TryApply(
            CreateMe(["core.warehouse.location.read"]),
            out var error));

        Assert.AreEqual(string.Empty, error);
        Assert.IsTrue(access.CanNavigate("internal-organization"));
    }

    private static InternalOrganizationService CreateService(
        HttpMessageHandler handler,
        ICanonicalIdempotencyKeyProvider keyProvider)
    {
        var endpoint = new CompanyEndpointProvider();
        endpoint.SetBaseUri(new Uri("https://company.example.test/"));
        var api = new CompanyApiClient(new HttpClient(handler), endpoint);
        return new InternalOrganizationService(
            api,
            new SessionAccessor(Token),
            keyProvider);
    }

    private static InternalMeData CreateMe(string[] permissions) =>
        new()
        {
            ActorId = "user:22222222-2222-4222-8222-222222222222",
            Roles = ["employee"],
            Permissions = permissions,
            Scopes = new AccessScopesData(),
            Session = new InternalAuthSessionData
            {
                LoginName = "nhanvien",
                EmployeeFullName = "Nhân viên",
                ExpiresAt = "2126-09-14T00:00:00.000Z"
            }
        };

    private static HttpResponseMessage Json(HttpStatusCode statusCode, string json) =>
        new(statusCode)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

    private sealed class SessionAccessor(string token) : IAuthenticatedSessionAccessor
    {
        public string? CurrentToken { get; } = token;
    }

    private sealed class DelegateHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, Task<HttpResponseMessage>> _handler;

        public DelegateHandler(Func<HttpRequestMessage, HttpResponseMessage> handler)
        {
            _handler = request => Task.FromResult(handler(request));
        }

        public DelegateHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> handler)
        {
            _handler = handler;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            _handler(request);
    }
}
