namespace CongTy.ApiClient;

public sealed class RequestIdHandler(IRequestIdProvider requestIds) : DelegatingHandler
{
    public const string HeaderName = "X-Request-ID";
    public static readonly HttpRequestOptionsKey<string> RequestIdOption = new("CongTy.RequestId");

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var requestId = request.Headers.TryGetValues(HeaderName, out var values)
            ? values.FirstOrDefault()
            : null;

        if (!requestIds.IsValid(requestId))
        {
            requestId = requestIds.Create();
            request.Headers.Remove(HeaderName);
            request.Headers.TryAddWithoutValidation(HeaderName, requestId);
        }

        request.Options.Set(RequestIdOption, requestId!);
        return base.SendAsync(request, cancellationToken);
    }
}
