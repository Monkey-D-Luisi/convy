using System.Net;
using System.Text;
using Convy.API.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;

namespace Convy.API.Tests;

public class McpClientMetadataValidatorTests
{
    [Fact]
    public async Task ValidateAsync_WithValidCimd_ReturnsMetadata()
    {
        using var httpClient = CreateHttpClient("""
        {
          "client_id": "https://chat.openai.com/mcp/client.json",
          "client_name": "ChatGPT",
          "redirect_uris": ["https://chat.openai.com/aip/callback"],
          "grant_types": ["authorization_code"],
          "response_types": ["code"],
          "token_endpoint_auth_method": "none"
        }
        """);
        var validator = new McpClientMetadataValidator(
            new StaticHttpClientFactory(httpClient),
            NullLogger<McpClientMetadataValidator>.Instance,
            new StaticDnsResolver(IPAddress.Parse("1.1.1.1")));

        var result = await validator.ValidateAsync(
            "https://chat.openai.com/mcp/client.json",
            "https://chat.openai.com/aip/callback",
            CancellationToken.None);

        result.IsValid.Should().BeTrue();
        result.Metadata!.ClientName.Should().Be("ChatGPT");
    }

    [Theory]
    [InlineData("http://chat.openai.com/mcp/client.json")]
    [InlineData("https://localhost/mcp/client.json")]
    [InlineData("https://127.0.0.1/mcp/client.json")]
    [InlineData("https://10.0.0.1/mcp/client.json")]
    [InlineData("https://0.0.0.0/mcp/client.json")]
    [InlineData("https://100.64.0.1/mcp/client.json")]
    [InlineData("https://198.18.0.1/mcp/client.json")]
    public async Task ValidateAsync_WithUnsafeClientId_ReturnsInvalid(string clientId)
    {
        using var httpClient = CreateHttpClient("{}");
        var validator = new McpClientMetadataValidator(
            new StaticHttpClientFactory(httpClient),
            NullLogger<McpClientMetadataValidator>.Instance,
            new StaticDnsResolver(IPAddress.Parse("1.1.1.1")));

        var result = await validator.ValidateAsync(clientId, "https://chat.openai.com/aip/callback", CancellationToken.None);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateAsync_WithIpv4MappedPrivateResolvedHost_ReturnsInvalid()
    {
        using var httpClient = CreateHttpClient("{}");
        var validator = new McpClientMetadataValidator(
            new StaticHttpClientFactory(httpClient),
            NullLogger<McpClientMetadataValidator>.Instance,
            new StaticDnsResolver(IPAddress.Parse("::ffff:10.0.0.5")));

        var result = await validator.ValidateAsync(
            "https://metadata.example.com/client.json",
            "https://chat.openai.com/aip/callback",
            CancellationToken.None);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Program_ShouldDisableAutomaticRedirectsForMcpClientMetadataFetches()
    {
        var source = File.ReadAllText(Path.Combine(FindRepoRoot(), "backend", "src", "Convy.API", "Program.cs"));

        source.Should().Contain("ConfigurePrimaryHttpMessageHandler");
        source.Should().Contain("AllowAutoRedirect = false");
    }

    [Fact]
    public async Task ValidateAsync_WithRedirectMismatch_ReturnsInvalid()
    {
        using var httpClient = CreateHttpClient("""
        {
          "client_id": "https://chat.openai.com/mcp/client.json",
          "client_name": "ChatGPT",
          "redirect_uris": ["https://chat.openai.com/aip/callback"]
        }
        """);
        var validator = new McpClientMetadataValidator(
            new StaticHttpClientFactory(httpClient),
            NullLogger<McpClientMetadataValidator>.Instance,
            new StaticDnsResolver(IPAddress.Parse("1.1.1.1")));

        var result = await validator.ValidateAsync(
            "https://chat.openai.com/mcp/client.json",
            "https://evil.example.com/callback",
            CancellationToken.None);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateAsync_WithPrivateResolvedHost_ReturnsInvalid()
    {
        using var httpClient = CreateHttpClient("{}");
        var validator = new McpClientMetadataValidator(
            new StaticHttpClientFactory(httpClient),
            NullLogger<McpClientMetadataValidator>.Instance,
            new StaticDnsResolver(IPAddress.Parse("10.0.0.5")));

        var result = await validator.ValidateAsync(
            "https://metadata.example.com/client.json",
            "https://chat.openai.com/aip/callback",
            CancellationToken.None);

        result.IsValid.Should().BeFalse();
    }

    private static HttpClient CreateHttpClient(string body)
    {
        var handler = new StaticHttpMessageHandler(body);
        return new HttpClient(handler)
        {
            BaseAddress = new Uri("https://chat.openai.com"),
        };
    }

    private sealed class StaticHttpClientFactory : IHttpClientFactory
    {
        private readonly HttpClient _client;

        public StaticHttpClientFactory(HttpClient client)
        {
            _client = client;
        }

        public HttpClient CreateClient(string name) => _client;
    }

    private sealed class StaticHttpMessageHandler : HttpMessageHandler
    {
        private readonly string _body;

        public StaticHttpMessageHandler(string body)
        {
            _body = body;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(_body, Encoding.UTF8, "application/json"),
            };

            return Task.FromResult(response);
        }
    }

    private sealed class StaticDnsResolver : IMcpClientMetadataDnsResolver
    {
        private readonly IReadOnlyList<IPAddress> _addresses;

        public StaticDnsResolver(params IPAddress[] addresses)
        {
            _addresses = addresses;
        }

        public Task<IReadOnlyList<IPAddress>> GetHostAddressesAsync(string host, CancellationToken cancellationToken) =>
            Task.FromResult(_addresses);
    }

    private static string FindRepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "backend", "src", "Convy.API", "Program.cs")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Repository root could not be found.");
    }
}
