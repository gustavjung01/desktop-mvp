using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace CongTy.ApiClient;

public sealed class SafeHttpLoggingHandler(
    ILogger<SafeHttpLoggingHandler> logger,
    ILogRedactor redactor) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        request.Options.TryGetValue(RequestIdHandler.RequestIdOption, out var requestId);

        try
        {
            var response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
            stopwatch.Stop();

            logger.LogInformation(
                "HTTP {Method} {Path} -> {StatusCode} in {DurationMs}ms requestId={RequestId}",
                request.Method.Method,
                SafePath(request.RequestUri),
                (int)response.StatusCode,
                stopwatch.ElapsedMilliseconds,
                redactor.Redact(requestId));

            return response;
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
        {
            stopwatch.Stop();
            logger.LogWarning(
                "HTTP {Method} {Path} failed in {DurationMs}ms requestId={RequestId} error={ErrorType}",
                request.Method.Method,
                SafePath(request.RequestUri),
                stopwatch.ElapsedMilliseconds,
                redactor.Redact(requestId),
                exception.GetType().Name);
            throw;
        }
    }

    private static string SafePath(Uri? uri)
    {
        if (uri is null)
        {
            return "/";
        }

        return string.IsNullOrEmpty(uri.AbsolutePath) ? "/" : uri.AbsolutePath;
    }
}
