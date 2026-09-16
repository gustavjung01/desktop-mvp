using System.Security.Cryptography;
using System.Text;
using CongTy.ApiClient;

namespace CongTy.Windows;

public sealed class WindowsSessionTokenStore(ISecureCredentialStore credentialStore) : ISessionTokenStore
{
    public async Task<string?> ReadAsync(Uri baseUri, CancellationToken cancellationToken = default)
    {
        var key = BuildCredentialKey(baseUri);
        var value = await credentialStore.ReadAsync(key, cancellationToken).ConfigureAwait(false);

        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (!value.StartsWith("nppusr.", StringComparison.Ordinal) || value.Length > 512)
        {
            await credentialStore.DeleteAsync(key, cancellationToken).ConfigureAwait(false);
            return null;
        }

        return value;
    }

    public Task WriteAsync(Uri baseUri, string token, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(token);
        if (!token.StartsWith("nppusr.", StringComparison.Ordinal) || token.Length > 512)
        {
            throw new ArgumentException("Session token is invalid.", nameof(token));
        }

        return credentialStore.WriteAsync(BuildCredentialKey(baseUri), token, cancellationToken);
    }

    public Task DeleteAsync(Uri baseUri, CancellationToken cancellationToken = default) =>
        credentialStore.DeleteAsync(BuildCredentialKey(baseUri), cancellationToken);

    public static string BuildCredentialKey(Uri baseUri)
    {
        ArgumentNullException.ThrowIfNull(baseUri);
        var authority = baseUri.GetLeftPart(UriPartial.Authority).TrimEnd('/').ToLowerInvariant();
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(authority));
        return $"session.{Convert.ToHexString(hash).ToLowerInvariant()[..32]}";
    }
}
