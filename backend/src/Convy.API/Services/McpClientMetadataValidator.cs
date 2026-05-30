using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;

namespace Convy.API.Services;

public class McpClientMetadataValidator
{
    private const int MaxMetadataBytes = 64 * 1024;
    private static readonly TimeSpan FetchTimeout = TimeSpan.FromSeconds(5);

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<McpClientMetadataValidator> _logger;
    private readonly IMcpClientMetadataDnsResolver _dnsResolver;
    private readonly HashSet<string> _allowedHosts;

    public McpClientMetadataValidator(
        IHttpClientFactory httpClientFactory,
        ILogger<McpClientMetadataValidator> logger,
        IMcpClientMetadataDnsResolver dnsResolver,
        IConfiguration configuration)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _dnsResolver = dnsResolver;
        _allowedHosts = configuration.GetSection("McpAuth:AllowedClientMetadataHosts")
            .Get<string[]>()?
            .Where(host => !string.IsNullOrWhiteSpace(host))
            .Select(host => host.Trim().TrimEnd('.'))
            .ToHashSet(StringComparer.OrdinalIgnoreCase)
            ?? [];
    }

    public async Task<McpClientMetadataValidationResult> ValidateAsync(
        string clientId,
        string redirectUri,
        CancellationToken cancellationToken)
    {
        var uriValidation = await ValidateClientMetadataUriAsync(clientId, cancellationToken);
        if (!uriValidation.IsSafe)
            return McpClientMetadataValidationResult.Invalid(uriValidation.Error ?? "invalid_client_id");

        var clientUri = uriValidation.ClientUri!;

        if (!Uri.TryCreate(redirectUri, UriKind.Absolute, out var redirect)
            || redirect.Scheme != Uri.UriSchemeHttps)
        {
            return McpClientMetadataValidationResult.Invalid("invalid_redirect_uri");
        }

        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(FetchTimeout);

            var client = _httpClientFactory.CreateClient("mcp-client-metadata");
            using var response = await client.GetAsync(clientUri, HttpCompletionOption.ResponseHeadersRead, timeout.Token);
            if (!response.IsSuccessStatusCode)
                return McpClientMetadataValidationResult.Invalid("client_metadata_unavailable");

            var payload = await ReadLimitedAsync(response.Content, timeout.Token);
            var metadata = JsonSerializer.Deserialize<McpClientMetadataDocument>(payload, McpClientMetadataJsonContext.Default.McpClientMetadataDocument);
            if (metadata is null)
                return McpClientMetadataValidationResult.Invalid("invalid_client_metadata");

            if (!string.Equals(metadata.ClientId, clientId, StringComparison.Ordinal))
                return McpClientMetadataValidationResult.Invalid("client_id_mismatch");

            if (!metadata.RedirectUris.Contains(redirectUri, StringComparer.Ordinal))
                return McpClientMetadataValidationResult.Invalid("redirect_uri_mismatch");

            return McpClientMetadataValidationResult.Valid(metadata);
        }
        catch (OperationCanceledException)
        {
            return McpClientMetadataValidationResult.Invalid("client_metadata_timeout");
        }
        catch (JsonException)
        {
            return McpClientMetadataValidationResult.Invalid("invalid_client_metadata");
        }
        catch (Exception ex) when (ex is HttpRequestException or IOException or SocketException)
        {
            _logger.LogWarning(ex, "Failed to fetch MCP client metadata.");
            return McpClientMetadataValidationResult.Invalid("client_metadata_unavailable");
        }
    }

    private async Task<(bool IsSafe, Uri? ClientUri, string? Error)> ValidateClientMetadataUriAsync(string clientId, CancellationToken cancellationToken)
    {
        if (!Uri.TryCreate(clientId, UriKind.Absolute, out var parsed))
            return (false, null, "invalid_client_id");

        if (parsed.Scheme != Uri.UriSchemeHttps)
            return (false, null, "invalid_client_id");

        if (_allowedHosts.Count == 0 || !_allowedHosts.Contains(parsed.Host.TrimEnd('.')))
            return (false, null, "client_metadata_host_not_allowed");

        if (string.IsNullOrWhiteSpace(parsed.AbsolutePath) || parsed.AbsolutePath == "/")
            return (false, null, "invalid_client_id");

        if (string.Equals(parsed.Host, "localhost", StringComparison.OrdinalIgnoreCase))
            return (false, null, "invalid_client_id");

        if (IPAddress.TryParse(parsed.Host, out var address) && IsPrivateOrLocalAddress(address))
            return (false, null, "invalid_client_id");

        if (!IPAddress.TryParse(parsed.Host, out _))
        {
            var resolvedAddresses = await _dnsResolver.GetHostAddressesAsync(parsed.Host, cancellationToken);
            if (resolvedAddresses.Count == 0 || resolvedAddresses.Any(IsPrivateOrLocalAddress))
                return (false, null, "invalid_client_id");
        }

        return (true, parsed, null);
    }

    private static bool IsPrivateOrLocalAddress(IPAddress address)
    {
        if (address.IsIPv4MappedToIPv6)
            address = address.MapToIPv4();

        if (IPAddress.IsLoopback(address))
            return true;

        var bytes = address.GetAddressBytes();
        if (address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
        {
            return bytes[0] == 0
                || bytes[0] == 10
                || bytes[0] == 127
                || bytes[0] == 100 && bytes[1] >= 64 && bytes[1] <= 127
                || bytes[0] == 169 && bytes[1] == 254
                || bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31
                || bytes[0] == 192 && bytes[1] == 0 && bytes[2] == 0
                || bytes[0] == 192 && bytes[1] == 0 && bytes[2] == 2
                || bytes[0] == 192 && bytes[1] == 168
                || bytes[0] == 198 && (bytes[1] == 18 || bytes[1] == 19)
                || bytes[0] == 198 && bytes[1] == 51 && bytes[2] == 100
                || bytes[0] == 203 && bytes[1] == 0 && bytes[2] == 113
                || bytes[0] >= 224;
        }

        if (address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
        {
            return address.Equals(IPAddress.IPv6None)
                || address.Equals(IPAddress.IPv6Any)
                || address.IsIPv6LinkLocal
                || address.IsIPv6SiteLocal
                || address.IsIPv6UniqueLocal
                || address.IsIPv6Multicast
                || bytes[0] == 0x20 && bytes[1] == 0x01 && bytes[2] == 0x0d && bytes[3] == 0xb8;
        }

        return true;
    }

    private static async Task<Stream> ReadLimitedAsync(HttpContent content, CancellationToken cancellationToken)
    {
        await using var source = await content.ReadAsStreamAsync(cancellationToken);
        var buffer = new MemoryStream();
        var bytes = new byte[8192];
        var total = 0;

        while (true)
        {
            var read = await source.ReadAsync(bytes, cancellationToken);
            if (read == 0)
                break;

            total += read;
            if (total > MaxMetadataBytes)
                throw new IOException("Client metadata document is too large.");

            await buffer.WriteAsync(bytes.AsMemory(0, read), cancellationToken);
        }

        buffer.Position = 0;
        return buffer;
    }
}

public record McpClientMetadataValidationResult(
    bool IsValid,
    McpClientMetadataDocument? Metadata,
    string? Error)
{
    public static McpClientMetadataValidationResult Valid(McpClientMetadataDocument metadata) => new(true, metadata, null);
    public static McpClientMetadataValidationResult Invalid(string error) => new(false, null, error);
}

public record McpClientMetadataDocument(
    [property: JsonPropertyName("client_id")] string ClientId,
    [property: JsonPropertyName("client_name")] string ClientName,
    [property: JsonPropertyName("redirect_uris")] IReadOnlyList<string> RedirectUris,
    [property: JsonPropertyName("grant_types")] IReadOnlyList<string>? GrantTypes,
    [property: JsonPropertyName("response_types")] IReadOnlyList<string>? ResponseTypes,
    [property: JsonPropertyName("token_endpoint_auth_method")] string? TokenEndpointAuthMethod);

[JsonSerializable(typeof(McpClientMetadataDocument))]
internal partial class McpClientMetadataJsonContext : JsonSerializerContext
{
}

public interface IMcpClientMetadataDnsResolver
{
    Task<IReadOnlyList<IPAddress>> GetHostAddressesAsync(string host, CancellationToken cancellationToken);
}

public class DnsMcpClientMetadataResolver : IMcpClientMetadataDnsResolver
{
    public async Task<IReadOnlyList<IPAddress>> GetHostAddressesAsync(string host, CancellationToken cancellationToken)
    {
        return await Dns.GetHostAddressesAsync(host, cancellationToken);
    }
}
