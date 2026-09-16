using System.Net;
using System.Net.Http.Headers;
using System.Text;
using CongTy.ApiClient;
using CongTy.Contracts;
using CongTy.Windows;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CongTy.UnitTests;

[TestClass]
public sealed class AuthAccessTests
{
    private const string Token = "nppusr.11111111-1111-4111-8111-111111111111.abcdefghijklmnopqrstuvwxyzABCDE1234567890";

    [TestMethod]
    public void ApiBaseUrl_RequiresHttpsOriginOnly()
    {
        Assert.IsTrue(CompanyEndpointProvider.TryNormalizeHttpsBaseUrl(
            "https://company.example.test/",
            out var valid,
            out var normalized,
            out _));
        Assert.AreEqual("https://company.example.test", normalized);
        Assert.AreEqual("https", valid!.Scheme);

        Assert.IsFalse(CompanyEndpointProvider.TryNormalizeHttpsBaseUrl(
            "http://company.example.test",
            out _,
            out _,
            out _));
        Assert.IsFalse(CompanyEndpointProvider.TryNormalizeHttpsBaseUrl(
            "https://user:pass@company.example.test",
            out _,
            out _,
            out _));
        Assert.IsFalse(CompanyEndpointProvider.TryNormalizeHttpsBaseUrl(
            "https://company.example.test/api",
            out _,
            out _,
            out _));
    }

    [TestMethod]
    public async Task InstallationSetup_ValidatesLiveAndReadyBeforeSaving()
    {
        var paths = new List<string>();
        var handler = new DelegateHandler(request =>
        {
            paths.Add(request.RequestUri!.AbsolutePath);
            var status = request.RequestUri.AbsolutePath == "/health/live" ? "ok" : "ready";
            return Json(HttpStatusCode.OK, $$"""
            {
              "data": { "status": "{{status}}" },
              "requestId": "req_health",
              "receivedAt": "2026-09-14T00:00:00.000Z"
            }
            """);
        });

        var endpoint = new CompanyEndpointProvider();
        var api = new CompanyApiClient(new HttpClient(handler), endpoint);
        var directory = Path.Combine(Path.GetTempPath(), $"congty-install-{Guid.NewGuid():N}");
        var settingsPath = Path.Combine(directory, "settings.json");

        try
        {
            var settingsStore = new JsonLocalSettingsStore(settingsPath);
            var state = new DesktopSettingsState(new DesktopSettings());
            var tokens = new FakeSessionTokenStore();
            var service = new InstallationProfileService(settingsStore, state, endpoint, api, tokens);

            var result = await service.ValidateAndSaveAsync(
                "Máy kế toán",
                "Công Ty Kiểm Thử",
                "https://company.example.test/");

            Assert.IsTrue(result.Success);
            CollectionAssert.AreEqual(new[] { "/health/live", "/health/ready" }, paths);
            Assert.AreEqual("https://company.example.test", state.Current.ApiBaseUrl);
            Assert.AreEqual("Công Ty Kiểm Thử", state.Current.CompanyDisplayName);
            Assert.IsTrue(service.IsConfigured);
        }
        finally
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }
    }

    [TestMethod]
    public async Task Login_VerifiesMeBeforePersistingAndKeepsCanonicalScopes()
    {
        var meSeen = false;
        var requests = new List<(string Method, string Path, AuthenticationHeaderValue? Authorization)>();

        var handler = new DelegateHandler(request =>
        {
            requests.Add((request.Method.Method, request.RequestUri!.AbsolutePath, request.Headers.Authorization));

            if (request.RequestUri.AbsolutePath == "/api/internal-auth/login")
            {
                return Json(HttpStatusCode.OK, $$"""
                {
                  "data": {
                    "token": "{{Token}}",
                    "session": {
                      "id": "11111111-1111-4111-8111-111111111111",
                      "expiresAt": "2126-09-14T00:00:00.000Z",
                      "sourceApp": "congty-desktop",
                      "accessChannel": "WEB"
                    },
                    "user": {
                      "id": "22222222-2222-4222-8222-222222222222",
                      "loginName": "nhanvien",
                      "employeeId": "33333333-3333-4333-8333-333333333333",
                      "employeeFullName": "Nhân viên kiểm thử",
                      "roles": ["sales"],
                      "permissions": ["core.customer.read"],
                      "scopes": {
                        "branchIds": ["44444444-4444-4444-8444-444444444444"],
                        "warehouseIds": ["55555555-5555-4555-8555-555555555555"],
                        "territoryIds": []
                      },
                      "ownerKind": null
                    }
                  },
                  "requestId": "req_login",
                  "receivedAt": "2026-09-14T00:00:00.000Z"
                }
                """);
            }

            meSeen = true;
            return Json(HttpStatusCode.OK, """
            {
              "data": {
                "actorId": "user:22222222-2222-4222-8222-222222222222",
                "employeeId": "33333333-3333-4333-8333-333333333333",
                "roles": ["sales"],
                "permissions": ["core.customer.read"],
                "scopes": {
                  "branchIds": ["44444444-4444-4444-8444-444444444444"],
                  "warehouseIds": ["55555555-5555-4555-8555-555555555555"],
                  "territoryIds": []
                },
                "sourceApp": "congty-desktop",
                "session": {
                  "id": "11111111-1111-4111-8111-111111111111",
                  "userId": "22222222-2222-4222-8222-222222222222",
                  "loginName": "nhanvien",
                  "employeeFullName": "Nhân viên kiểm thử",
                  "expiresAt": "2126-09-14T00:00:00.000Z",
                  "sourceApp": "congty-desktop",
                  "ownerKind": null
                }
              },
              "requestId": "req_me",
              "receivedAt": "2026-09-14T00:00:00.000Z"
            }
            """);
        });

        var endpoint = ConfiguredEndpoint();
        var api = new CompanyApiClient(new HttpClient(handler), endpoint);
        var access = new AccessStateService();
        var tokens = new FakeSessionTokenStore(() => meSeen);
        var auth = new AuthenticationService(api, endpoint, tokens, access);

        var result = await auth.LoginAsync("nhanvien", "correct-horse-battery-staple");

        Assert.AreEqual(LoginAttemptKind.Succeeded, result.Kind);
        Assert.AreEqual(Token, tokens.Token);
        Assert.HasCount(2, requests);
        Assert.AreEqual("/api/internal-auth/login", requests[0].Path);
        Assert.IsNull(requests[0].Authorization);
        Assert.AreEqual("/api/internal-auth/me", requests[1].Path);
        var authorization = requests[1].Authorization!;
        Assert.AreEqual("Bearer", authorization.Scheme);
        Assert.AreEqual(Token, authorization.Parameter);
        Assert.IsTrue(access.HasPermission("core.customer.read"));
        Assert.IsFalse(access.HasPermission("core.customer.write"));
        CollectionAssert.AreEqual(
            new[] { "44444444-4444-4444-8444-444444444444" },
            access.Current.Scopes.BranchIds);
        CollectionAssert.AreEqual(
            new[] { "55555555-5555-4555-8555-555555555555" },
            access.Current.Scopes.WarehouseIds);
    }

    [TestMethod]
    public async Task OwnerChallenge_DoesNotCreateLocalSessionBeforeVerification()
    {
        var handler = new DelegateHandler(_ => Json(HttpStatusCode.Unauthorized, """
        {
          "error": {
            "code": "INTERNAL_AUTH_OWNER_CHALLENGE_REQUIRED",
            "message": "Verification required",
            "details": {},
            "retryable": false
          },
          "requestId": "req_owner",
          "receivedAt": "2026-09-14T00:00:00.000Z"
        }
        """));

        var endpoint = ConfiguredEndpoint();
        var api = new CompanyApiClient(new HttpClient(handler), endpoint);
        var access = new AccessStateService();
        var tokens = new FakeSessionTokenStore();
        var auth = new AuthenticationService(api, endpoint, tokens, access);

        var result = await auth.LoginAsync("owner@example.test", "correct-horse-battery-staple");

        Assert.AreEqual(LoginAttemptKind.ChallengeRequired, result.Kind);
        Assert.IsNull(tokens.Token);
        Assert.IsFalse(access.Current.IsAuthenticated);
    }

    [TestMethod]
    public async Task RestoreUnauthorized_ClearsStoredSessionAndAccess()
    {
        var handler = new DelegateHandler(_ => Json(HttpStatusCode.Unauthorized, """
        {
          "error": {
            "code": "INTERNAL_AUTH_SESSION_EXPIRED",
            "message": "Phiên đăng nhập đã hết hạn",
            "details": {},
            "retryable": false
          },
          "requestId": "req_expired",
          "receivedAt": "2026-09-14T00:00:00.000Z"
        }
        """));

        var endpoint = ConfiguredEndpoint();
        var api = new CompanyApiClient(new HttpClient(handler), endpoint);
        var access = new AccessStateService();
        var tokens = new FakeSessionTokenStore { Token = Token };
        var auth = new AuthenticationService(api, endpoint, tokens, access);

        var result = await auth.RestoreAsync();

        Assert.AreEqual(SessionRestoreKind.ExpiredOrInvalid, result.Kind);
        Assert.IsNull(tokens.Token);
        Assert.IsFalse(access.Current.IsAuthenticated);
    }

    [TestMethod]
    public void AccessPolicy_IsDenyByDefault()
    {
        var access = new AccessStateService();
        var applied = access.TryApply(new InternalMeData
        {
            ActorId = "user:22222222-2222-4222-8222-222222222222",
            EmployeeId = "33333333-3333-4333-8333-333333333333",
            Roles = ["sales"],
            Permissions = ["core.customer.read"],
            Scopes = new AccessScopesData
            {
                BranchIds = ["44444444-4444-4444-8444-444444444444"],
                WarehouseIds = ["55555555-5555-4555-8555-555555555555"]
            },
            Session = new InternalAuthSessionData
            {
                LoginName = "nhanvien",
                EmployeeFullName = "Nhân viên kiểm thử",
                ExpiresAt = "2126-09-14T00:00:00.000Z"
            }
        }, out _);

        Assert.IsTrue(applied);
        Assert.IsTrue(access.CanNavigate("home"));
        Assert.IsFalse(access.CanNavigate("sales"));
        Assert.IsTrue(access.CanUseAction("core.customer.read"));
        Assert.IsFalse(access.CanUseAction("core.customer.write"));
        Assert.IsFalse(access.CanUseAction(null));
    }

    private static CompanyEndpointProvider ConfiguredEndpoint()
    {
        var endpoint = new CompanyEndpointProvider();
        endpoint.SetBaseUri(new Uri("https://company.example.test/"));
        return endpoint;
    }

    private static HttpResponseMessage Json(HttpStatusCode statusCode, string json) =>
        new(statusCode)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

    private sealed class DelegateHandler(Func<HttpRequestMessage, HttpResponseMessage> handler) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            Task.FromResult(handler(request));
    }

    private sealed class FakeSessionTokenStore(Func<bool>? canWrite = null) : ISessionTokenStore
    {
        public string? Token { get; set; }

        public Task<string?> ReadAsync(Uri baseUri, CancellationToken cancellationToken = default) =>
            Task.FromResult(Token);

        public Task WriteAsync(Uri baseUri, string token, CancellationToken cancellationToken = default)
        {
            if (canWrite is not null && !canWrite())
            {
                throw new InvalidOperationException("Token was persisted before /me verification.");
            }

            Token = token;
            return Task.CompletedTask;
        }

        public Task DeleteAsync(Uri baseUri, CancellationToken cancellationToken = default)
        {
            Token = null;
            return Task.CompletedTask;
        }
    }
}
