using System.Net;
using System.Text.Json;
using CongTy.ApiClient;
using CongTy.Contracts;
using CongTy.Windows;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CongTy.UnitTests;

[TestClass]
public sealed class FoundationTests
{
    [TestMethod]
    public void RequestId_IsCanonicalAndUnique()
    {
        var provider = new RequestIdProvider();

        var first = provider.Create();
        var second = provider.Create();

        Assert.IsTrue(provider.IsValid(first));
        Assert.IsTrue(provider.IsValid(second));
        Assert.AreNotEqual(first, second);
        Assert.IsTrue(first.StartsWith("desktop_", StringComparison.Ordinal));
    }

    [TestMethod]
    public void CanonicalErrorEnvelope_MatchesBackendContract()
    {
        const string json = """
        {
          "error": {
            "code": "CONFLICT",
            "message": "Dữ liệu đã thay đổi.",
            "details": { "version": 2 },
            "retryable": false
          },
          "requestId": "req_abc123",
          "receivedAt": "2026-09-14T00:00:00.000Z"
        }
        """;

        var envelope = JsonSerializer.Deserialize<ApiErrorEnvelope>(json, new JsonSerializerOptions(JsonSerializerDefaults.Web));

        Assert.IsNotNull(envelope);
        Assert.AreEqual("CONFLICT", envelope.Error.Code);
        Assert.AreEqual("req_abc123", envelope.RequestId);
        Assert.IsFalse(envelope.Error.Retryable);
        Assert.AreEqual(2, envelope.Error.Details.GetProperty("version").GetInt32());
    }

    [TestMethod]
    public void LogRedactor_RemovesSecretsAndKeepsUsefulText()
    {
        var redactor = new LogRedactor();
        var credentialedDatabaseUrl = string.Concat("postgres://", "sample-user:", "sample-pass@", "host.invalid/db");
        var input = $"authorization=abc123 Bearer token.value database_url={credentialedDatabaseUrl}";

        var output = redactor.Redact(input);

        Assert.IsFalse(output.Contains("abc123", StringComparison.Ordinal));
        Assert.IsFalse(output.Contains("token.value", StringComparison.Ordinal));
        Assert.IsFalse(output.Contains("sample-user:sample-pass", StringComparison.Ordinal));
        Assert.IsTrue(output.Contains("[REDACTED]", StringComparison.Ordinal));
    }

    [TestMethod]
    public async Task LocalSettings_StoresOnlyClientSafeProfile()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"congty-desktop-{Guid.NewGuid():N}");
        var filePath = Path.Combine(directory, "settings.json");

        try
        {
            var store = new JsonLocalSettingsStore(filePath);
            var settings = new DesktopSettings
            {
                InstallationName = "Cấu hình kiểm thử",
                CompanyDisplayName = "Công Ty kiểm thử",
                ApiBaseUrl = "https://company.example.test",
                Theme = "Dark"
            };

            await store.SaveAsync(settings);
            var roundTrip = await store.LoadAsync();
            var persisted = await File.ReadAllTextAsync(filePath);

            Assert.AreEqual(settings, roundTrip);
            Assert.IsFalse(persisted.Contains("password", StringComparison.OrdinalIgnoreCase));
            Assert.IsFalse(persisted.Contains("secret", StringComparison.OrdinalIgnoreCase));
            Assert.IsFalse(persisted.Contains("database_url", StringComparison.OrdinalIgnoreCase));
            Assert.IsFalse(persisted.Contains("nppusr.", StringComparison.OrdinalIgnoreCase));
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
    public void SecureCredentialKey_RejectsUnsafeNames()
    {
        Assert.IsTrue(WindowsCredentialStore.IsValidKey("session.primary"));
        Assert.IsFalse(WindowsCredentialStore.IsValidKey("../session"));
        Assert.IsFalse(WindowsCredentialStore.IsValidKey("session token"));
    }

    [TestMethod]
    public void SessionCredentialKey_IsInstallationSpecificAndSafe()
    {
        var first = WindowsSessionTokenStore.BuildCredentialKey(new Uri("https://one.example.test/"));
        var second = WindowsSessionTokenStore.BuildCredentialKey(new Uri("https://two.example.test/"));

        Assert.AreNotEqual(first, second);
        Assert.IsTrue(WindowsCredentialStore.IsValidKey(first));
        Assert.IsTrue(WindowsCredentialStore.IsValidKey(second));
    }

    [TestMethod]
    public async Task CompanyApiClient_ThrowsCanonicalApiException()
    {
        var handler = new StubHandler(new HttpResponseMessage(HttpStatusCode.Conflict)
        {
            Content = new StringContent(
                """
                {
                  "error": {
                    "code": "ORDER_CONFLICT",
                    "message": "Đơn hàng đã thay đổi.",
                    "details": {},
                    "retryable": false
                  },
                  "requestId": "req_conflict",
                  "receivedAt": "2026-09-14T00:00:00.000Z"
                }
                """,
                System.Text.Encoding.UTF8,
                "application/json")
        });

        var endpoints = new CompanyEndpointProvider();
        endpoints.SetBaseUri(new Uri("https://company.example.test/"));
        var client = new CompanyApiClient(new HttpClient(handler), endpoints);

        CanonicalApiException? captured = null;
        try
        {
            await client.GetDataAsync<object>("/api/example");
        }
        catch (CanonicalApiException exception)
        {
            captured = exception;
        }

        Assert.IsNotNull(captured);
        Assert.AreEqual(HttpStatusCode.Conflict, captured.StatusCode);
        Assert.AreEqual("ORDER_CONFLICT", captured.Code);
        Assert.AreEqual("req_conflict", captured.RequestId);
        Assert.IsFalse(captured.Retryable);
    }

    private sealed class StubHandler(HttpResponseMessage response) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(response);
    }
}
