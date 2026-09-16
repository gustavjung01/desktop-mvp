using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using CongTy.ApiClient;
using CongTy.Contracts;
using CongTy.Desktop.Partners;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CongTy.UnitTests;

[TestClass]
public sealed class PartnerTests
{
    private const string Token = "nppusr.11111111-1111-4111-8111-111111111111.abcdefghijklmnopqrstuvwxyzABCDE1234567890";

    [TestMethod]
    public void PartnerNavigation_IsPermissionDrivenAndDenyByDefault()
    {
        var access = new AccessStateService();

        Assert.IsTrue(access.TryApply(CreateMe(["core.customer.read"]), out var error));
        Assert.AreEqual(string.Empty, error);
        Assert.IsTrue(access.CanNavigate("partners"));

        access.Clear();

        Assert.IsTrue(access.TryApply(CreateMe(["core.supplier.read"]), out error));
        Assert.AreEqual(string.Empty, error);
        Assert.IsTrue(access.CanNavigate("partners"));

        access.Clear();

        Assert.IsTrue(access.TryApply(CreateMe(["core.product.read"]), out error));
        Assert.AreEqual(string.Empty, error);
        Assert.IsFalse(access.CanNavigate("partners"));
    }

    [TestMethod]
    public async Task CreateCustomer_UsesDirectBackendAndReusesCallerIdempotencyKey()
    {
        var requests = new List<(string Path, AuthenticationHeaderValue? Authorization, string? IdempotencyKey)>();
        var handler = new DelegateHandler(request =>
        {
            requests.Add((
                request.RequestUri!.AbsolutePath,
                request.Headers.Authorization,
                request.Headers.TryGetValues("Idempotency-Key", out var values) ? values.Single() : null));

            return Json(HttpStatusCode.Created, """
            {
              "data": {
                "id": "22222222-2222-4222-8222-222222222222",
                "code": "KH001",
                "name": "Khách hàng kiểm thử",
                "group_id": null,
                "group_name": null,
                "responsible_employee_id": null,
                "responsible_employee_name": null,
                "phone": null,
                "email": null,
                "tax_code": null,
                "payment_terms_days": 0,
                "credit_limit": "0",
                "notes": null,
                "is_active": true,
                "created_at": "2026-09-14T00:00:00.000Z",
                "updated_at": "2026-09-14T00:00:00.000Z"
              },
              "requestId": "req_customer",
              "receivedAt": "2026-09-14T00:00:00.000Z"
            }
            """);
        });

        var keys = new CanonicalIdempotencyKeyProvider();
        var operationKey = keys.Create("customer-create");
        var service = CreateService(handler, keys);
        var payload = new CustomerCreateRequest(
            "KH001",
            "Khách hàng kiểm thử",
            null,
            null,
            null,
            null,
            null,
            0,
            "0",
            null);

        await service.CreateCustomerAsync(payload, operationKey);
        await service.CreateCustomerAsync(payload, operationKey);

        Assert.HasCount(2, requests);
        Assert.AreEqual("/api/customers", requests[0].Path);
        var authorization = requests[0].Authorization;
        Assert.IsNotNull(authorization);
        Assert.AreEqual("Bearer", authorization.Scheme);
        Assert.AreEqual(Token, authorization.Parameter);
        Assert.AreEqual(operationKey, requests[0].IdempotencyKey);
        Assert.AreEqual(operationKey, requests[1].IdempotencyKey);
    }

    [TestMethod]
    public async Task BulkCustomerUpdate_UsesPatchBackendOperationKeyAndExpectedVersion()
    {
        HttpMethod? method = null;
        string? idempotencyKey = null;
        string? body = null;

        var handler = new DelegateHandler(async request =>
        {
            method = request.Method;
            idempotencyKey = request.Headers.TryGetValues("Idempotency-Key", out var values)
                ? values.Single()
                : null;
            body = request.Content is null
                ? null
                : await request.Content.ReadAsStringAsync();

            return Json(HttpStatusCode.OK, """
            {
              "data": {
                "updated": 1,
                "skipped": 0,
                "unchanged": 0,
                "rows": []
              },
              "requestId": "req_bulk",
              "receivedAt": "2026-09-14T00:00:00.000Z"
            }
            """);
        });

        var keys = new CanonicalIdempotencyKeyProvider();
        var service = CreateService(handler, keys);
        var operationKey = keys.Create("customer-bulk-update");
        const string version = "2026-09-14T00:00:00.000Z";

        await service.ApplyCustomerBulkUpdateAsync(
            new CustomerBulkRequest
            {
                DryRun = false,
                Mappings = ["CUSTOMER_CODE", "PHONE"],
                Rows =
                [
                    new CustomerBulkSourceRow
                    {
                        RowNumber = 2,
                        Cells = ["KH001", "0909000000"],
                        ExpectedUpdatedAt = version
                    }
                ]
            },
            operationKey);

        Assert.AreEqual(HttpMethod.Patch, method);
        Assert.AreEqual(operationKey, idempotencyKey);
        Assert.IsNotNull(body);

        using var document = JsonDocument.Parse(body);
        Assert.IsFalse(document.RootElement.GetProperty("dryRun").GetBoolean());
        var firstRow = document.RootElement.GetProperty("rows")[0];
        Assert.AreEqual(version, firstRow.GetProperty("expectedUpdatedAt").GetString());
    }

    [TestMethod]
    public async Task CreateSupplierContact_UsesCanonicalChildPathAndKey()
    {
        string? path = null;
        string? key = null;

        var handler = new DelegateHandler(request =>
        {
            path = request.RequestUri!.AbsolutePath;
            key = request.Headers.TryGetValues("Idempotency-Key", out var values)
                ? values.Single()
                : null;

            return Json(HttpStatusCode.Created, """
            {
              "data": {
                "id": "33333333-3333-4333-8333-333333333333",
                "supplier_id": "44444444-4444-4444-8444-444444444444",
                "contact_name": "Nguyễn Văn B",
                "contact_title": "Kinh doanh",
                "phone": "0909000000",
                "email": "b@example.test",
                "is_primary": true,
                "is_active": true,
                "created_at": "2026-09-14T00:00:00.000Z",
                "updated_at": "2026-09-14T00:00:00.000Z"
              },
              "requestId": "req_supplier_contact",
              "receivedAt": "2026-09-14T00:00:00.000Z"
            }
            """);
        });

        var keys = new CanonicalIdempotencyKeyProvider();
        var service = CreateService(handler, keys);
        var operationKey = keys.Create("supplier-contact-create");

        await service.CreateSupplierContactAsync(
            "44444444-4444-4444-8444-444444444444",
            new SupplierContactCreateRequest(
                "Nguyễn Văn B",
                "Kinh doanh",
                "0909000000",
                "b@example.test",
                true),
            operationKey);

        Assert.AreEqual(
            "/api/suppliers/44444444-4444-4444-8444-444444444444/contacts",
            path);
        Assert.AreEqual(operationKey, key);
    }

    [TestMethod]
    public async Task CustomerOverview_UsesCanonicalDirectReadEndpoint()
    {
        string? pathAndQuery = null;
        var handler = new DelegateHandler(request =>
        {
            pathAndQuery = request.RequestUri!.PathAndQuery;
            return Json(HttpStatusCode.OK, """
            {
              "data": {
                "customer": {
                  "id": "22222222-2222-4222-8222-222222222222",
                  "code": "KH001",
                  "name": "Khách hàng kiểm thử",
                  "group_id": null,
                  "group_name": null,
                  "responsible_employee_id": null,
                  "responsible_employee_name": null,
                  "phone": null,
                  "email": null,
                  "tax_code": null,
                  "payment_terms_days": 15,
                  "credit_limit": "1000000",
                  "notes": null,
                  "is_active": true,
                  "created_at": "2026-09-14T00:00:00.000Z",
                  "updated_at": "2026-09-14T00:00:00.000Z"
                },
                "period": "90d",
                "sales": {
                  "period": "90d",
                  "currencyCode": "VND",
                  "revenue": "500000",
                  "orderCount": "3",
                  "lastPurchaseAt": "2026-09-13T00:00:00.000Z"
                },
                "receivable": null,
                "permissions": {
                  "sales": true,
                  "receivable": false
                }
              },
              "requestId": "req_overview",
              "receivedAt": "2026-09-14T00:00:00.000Z"
            }
            """);
        });

        var service = CreateService(handler, new CanonicalIdempotencyKeyProvider());
        var overview = await service.GetCustomerOverviewAsync(
            "22222222-2222-4222-8222-222222222222",
            "90d");

        Assert.AreEqual(
            "/api/customers/22222222-2222-4222-8222-222222222222/overview?period=90d",
            pathAndQuery);
        Assert.IsTrue(overview.Permissions.Sales);
        Assert.IsFalse(overview.Permissions.Receivable);
        Assert.AreEqual("500000", overview.Sales!.Revenue);
    }

    [TestMethod]
    public async Task PrepareCustomerMedia_UsesSameCanonicalMediaPathAndIdempotencyKey()
    {
        string? path = null;
        string? key = null;
        var handler = new DelegateHandler(request =>
        {
            path = request.RequestUri!.AbsolutePath;
            key = request.Headers.TryGetValues("Idempotency-Key", out var values)
                ? values.Single()
                : null;

            return Json(HttpStatusCode.Created, """
            {
              "data": {
                "mediaId": "66666666-6666-4666-8666-666666666666",
                "putUrl": "https://storage.example.test/upload?signature=redacted",
                "mimeType": "image/png",
                "expiresIn": 300
              },
              "requestId": "req_media_prepare",
              "receivedAt": "2026-09-14T00:00:00.000Z"
            }
            """);
        });

        var keys = new CanonicalIdempotencyKeyProvider();
        var service = CreateService(handler, keys);
        var operationKey = keys.Create("desktop-customer-media-prepare");

        var prepared = await service.PrepareCustomerMediaAsync(
            "22222222-2222-4222-8222-222222222222",
            new CustomerMediaPrepareRequest(
                "prepare",
                Guid.NewGuid().ToString("N"),
                "image/png",
                1024),
            operationKey);

        Assert.AreEqual(
            "/api/customers/22222222-2222-4222-8222-222222222222/media",
            path);
        Assert.AreEqual(operationKey, key);
        Assert.AreEqual("image/png", prepared.MimeType);
    }

    [TestMethod]
    public async Task Customer360History_UsesCanonicalDirectEndpoints()
    {
        var paths = new List<string>();
        var handler = new DelegateHandler(request =>
        {
            paths.Add(request.RequestUri!.PathAndQuery);
            var path = request.RequestUri.AbsolutePath;
            var json = path switch
            {
                var value when value.EndsWith("/purchased-items", StringComparison.Ordinal) => """
                {
                  "data": {
                    "period": "90d",
                    "currencyCode": "VND",
                    "search": "",
                    "limit": 50,
                    "offset": 0,
                    "total": "0",
                    "items": []
                  }
                }
                """,
                "/api/sales-orders" => """{"data": []}""",
                "/api/receivables" => """{"data": []}""",
                "/api/customer-payments" => """{"data": []}""",
                var value when value.EndsWith("/delivery-returns", StringComparison.Ordinal) => """
                {
                  "data": {
                    "permissions": {
                      "deliveryOrders": true,
                      "deliveryAttempts": true,
                      "returns": true
                    },
                    "deliveries": {
                      "offset": 0,
                      "limit": 20,
                      "hasPrevious": false,
                      "hasNext": false,
                      "items": []
                    },
                    "returns": {
                      "offset": 0,
                      "limit": 20,
                      "hasPrevious": false,
                      "hasNext": false,
                      "items": []
                    }
                  }
                }
                """,
                _ => throw new InvalidOperationException($"Unexpected test endpoint: {request.RequestUri}")
            };
            return Json(HttpStatusCode.OK, json);
        });

        var service = CreateService(handler, new CanonicalIdempotencyKeyProvider());
        const string customerId = "22222222-2222-4222-8222-222222222222";

        await service.GetCustomerPurchasedItemsAsync(customerId, "90d", "", 50, 0);
        await service.ListCustomerOrdersAsync(customerId, "", 21, 0);
        await service.ListCustomerReceivablesAsync(customerId, 21, 0);
        await service.ListCustomerPaymentsAsync(customerId, 21, 0);
        var deliveryReturns = await service.GetCustomerDeliveryReturnsAsync(customerId, 20, 0, 20, 0);

        CollectionAssert.Contains(
            paths,
            $"/api/customers/{customerId}/purchased-items?period=90d&limit=50&offset=0");
        CollectionAssert.Contains(
            paths,
            $"/api/sales-orders?customerId={customerId}&limit=21&offset=0");
        CollectionAssert.Contains(
            paths,
            $"/api/receivables?customerId={customerId}&limit=21&offset=0");
        CollectionAssert.Contains(
            paths,
            $"/api/customer-payments?customerId={customerId}&limit=21&offset=0");
        CollectionAssert.Contains(
            paths,
            $"/api/customers/{customerId}/delivery-returns?deliveryLimit=20&deliveryOffset=0&returnLimit=20&returnOffset=0");
        Assert.IsTrue(deliveryReturns.Permissions.DeliveryOrders);
        Assert.IsTrue(deliveryReturns.Permissions.Returns);
    }

    [TestMethod]
    public async Task CreateDefaultAddresses_UseCanonicalChildPathsAndCallerKeys()
    {
        var requests = new List<(string Path, string? Key)>();
        var handler = new DelegateHandler(request =>
        {
            requests.Add((
                request.RequestUri!.AbsolutePath,
                request.Headers.TryGetValues("Idempotency-Key", out var values) ? values.Single() : null));

            if (request.RequestUri.AbsolutePath.Contains("/customers/", StringComparison.Ordinal))
            {
                return Json(HttpStatusCode.Created, """
                {
                  "data": {
                    "id": "77777777-7777-4777-8777-777777777777",
                    "customer_id": "22222222-2222-4222-8222-222222222222",
                    "label": "Địa chỉ giao dịch",
                    "recipient_name": "Khách hàng kiểm thử",
                    "phone": null,
                    "address_line1": "1 Nguyễn Huệ",
                    "address_line2": null,
                    "ward": "Bến Nghé",
                    "district": "Quận 1",
                    "province": "TP Hồ Chí Minh",
                    "postal_code": null,
                    "country_code": "VN",
                    "location_url": null,
                    "is_default": true,
                    "is_active": true,
                    "created_at": "2026-09-14T00:00:00.000Z",
                    "updated_at": "2026-09-14T00:00:00.000Z"
                  }
                }
                """);
            }

            return Json(HttpStatusCode.Created, """
            {
              "data": {
                "id": "88888888-8888-4888-8888-888888888888",
                "supplier_id": "44444444-4444-4444-8444-444444444444",
                "address_type": "business",
                "street": "2 Lê Lợi",
                "city": "Bến Nghé",
                "province": "TP Hồ Chí Minh",
                "postal_code": null,
                "country": "Việt Nam",
                "is_primary": true,
                "is_active": true,
                "created_at": "2026-09-14T00:00:00.000Z",
                "updated_at": "2026-09-14T00:00:00.000Z"
              }
            }
            """);
        });

        var keys = new CanonicalIdempotencyKeyProvider();
        var service = CreateService(handler, keys);
        var customerKey = keys.Create("customer-address-create");
        var supplierKey = keys.Create("supplier-address-create");

        await service.CreateCustomerAddressAsync(
            "22222222-2222-4222-8222-222222222222",
            new CustomerAddressCreateRequest(
                "Địa chỉ giao dịch",
                "Khách hàng kiểm thử",
                null,
                null,
                "1 Nguyễn Huệ",
                null,
                "Bến Nghé",
                "Quận 1",
                "TP Hồ Chí Minh",
                null,
                "VN",
                true),
            customerKey);

        await service.CreateSupplierAddressAsync(
            "44444444-4444-4444-8444-444444444444",
            new SupplierAddressCreateRequest(
                "business",
                "2 Lê Lợi",
                "Bến Nghé",
                "TP Hồ Chí Minh",
                null,
                "Việt Nam",
                true),
            supplierKey);

        Assert.AreEqual(
            "/api/customers/22222222-2222-4222-8222-222222222222/addresses",
            requests[0].Path);
        Assert.AreEqual(customerKey, requests[0].Key);
        Assert.AreEqual(
            "/api/suppliers/44444444-4444-4444-8444-444444444444/addresses",
            requests[1].Path);
        Assert.AreEqual(supplierKey, requests[1].Key);
    }

    [TestMethod]
    public async Task SpreadsheetReader_ReadsQuotedCsvWithoutBusinessMutation()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"congty-partners-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        var filePath = Path.Combine(directory, "customers.csv");

        try
        {
            await File.WriteAllTextAsync(
                filePath,
                """
                Mã khách,Tên khách,Điện thoại
                KH001,"Công ty A, Chi nhánh 1",0909000000
                """,
                Encoding.UTF8);

            var rows = await SpreadsheetMatrixReader.ReadAsync(filePath);

            Assert.HasCount(2, rows);
            CollectionAssert.AreEqual(
                new[] { "Mã khách", "Tên khách", "Điện thoại" },
                rows[0]);
            CollectionAssert.AreEqual(
                new[] { "KH001", "Công ty A, Chi nhánh 1", "0909000000" },
                rows[1]);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [TestMethod]
    public async Task ImageMetadataReader_ReadsPngDimensionsAndMimeType()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"congty-media-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        var filePath = Path.Combine(directory, "customer.png");

        try
        {
            var bytes = new byte[24];
            bytes[0] = 0x89;
            bytes[1] = 0x50;
            bytes[2] = 0x4E;
            bytes[3] = 0x47;
            bytes[4] = 0x0D;
            bytes[5] = 0x0A;
            bytes[6] = 0x1A;
            bytes[7] = 0x0A;
            WriteBigEndian(bytes, 16, 640);
            WriteBigEndian(bytes, 20, 480);
            await File.WriteAllBytesAsync(filePath, bytes);

            var result = await ImageMetadataReader.ReadAsync(filePath);

            Assert.AreEqual("image/png", result.MimeType);
            Assert.AreEqual(640, result.Width);
            Assert.AreEqual(480, result.Height);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static PartnerService CreateService(
        HttpMessageHandler handler,
        ICanonicalIdempotencyKeyProvider keyProvider)
    {
        var endpoint = new CompanyEndpointProvider();
        endpoint.SetBaseUri(new Uri("https://company.example.test/"));
        var api = new CompanyApiClient(new HttpClient(handler), endpoint);
        return new PartnerService(api, new SessionAccessor(Token), keyProvider);
    }

    private static InternalMeData CreateMe(string[] permissions) =>
        new()
        {
            ActorId = "user:55555555-5555-4555-8555-555555555555",
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

    private static void WriteBigEndian(byte[] buffer, int offset, int value)
    {
        buffer[offset] = (byte)((value >> 24) & 0xFF);
        buffer[offset + 1] = (byte)((value >> 16) & 0xFF);
        buffer[offset + 2] = (byte)((value >> 8) & 0xFF);
        buffer[offset + 3] = (byte)(value & 0xFF);
    }

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
