using Convy.API.Authorization;
using Convy.API.Endpoints;
using Convy.API.Middleware;
using Convy.API.Services;
using Convy.Application;
using Convy.Application.Common.Interfaces;
using Convy.Infrastructure;
using Convy.Infrastructure.Hubs;
using Convy.Infrastructure.Persistence;
using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// Compose connection string from managed host environment variables.
var dbHost = Environment.GetEnvironmentVariable("DB_HOST");
if (!string.IsNullOrEmpty(dbHost))
{
    var dbName = Environment.GetEnvironmentVariable("DB_NAME") ?? "convy";
    var dbUser = Environment.GetEnvironmentVariable("DB_USER") ?? "convy";
    var dbPassword = Environment.GetEnvironmentVariable("DB_PASSWORD") ?? "";
    var connectionString = $"Host={dbHost};Port=5432;Database={dbName};Username={dbUser};Password={dbPassword}";
    builder.Configuration["ConnectionStrings:DefaultConnection"] = connectionString;
}

// Health checks
builder.Services.AddHealthChecks()
    .AddDbContextCheck<ConvyDbContext>("db");

// Reverse proxy headers must be applied before HTTPS redirection.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

// OpenAPI
builder.Services.AddOpenApi();

// SignalR
builder.Services.AddSignalR()
    .AddJsonProtocol(options =>
    {
        options.PayloadSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

// JSON serialization — serialize enums as strings
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

// Authentication: route Firebase and Convy MCP JWTs behind the normal Bearer header.
builder.Services.AddAuthentication(AuthSchemes.DefaultBearer)
    .AddPolicyScheme(AuthSchemes.DefaultBearer, AuthSchemes.DefaultBearer, options =>
    {
        options.ForwardDefaultSelector = context =>
        {
            var authorization = context.Request.Headers.Authorization.ToString();
            return LooksLikeMcpAccessToken(authorization)
                ? AuthSchemes.McpBearer
                : AuthSchemes.FirebaseBearer;
        };
    })
    .AddJwtBearer(AuthSchemes.FirebaseBearer, options =>
    {
        var projectId = builder.Configuration["Firebase:ProjectId"];
        options.Authority = $"https://securetoken.google.com/{projectId}";
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = $"https://securetoken.google.com/{projectId}",
            ValidateAudience = true,
            ValidAudience = projectId,
            ValidateLifetime = true
        };
    })
    .AddJwtBearer(AuthSchemes.McpBearer, options =>
    {
        var issuer = builder.Configuration["McpAuth:Issuer"] ?? "https://auth.convyapp.com";
        var audience = builder.Configuration["McpAuth:Audience"] ?? "https://mcp.convyapp.com";
        SecurityKey publicKey = (SecurityKey?)McpJwtKeyLoader.LoadPublicKey(builder.Configuration)
            ?? new RsaSecurityKey(RSA.Create(2048));

        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = issuer,
            ValidateAudience = true,
            ValidAudience = audience,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = publicKey,
            ClockSkew = TimeSpan.FromMinutes(1)
        };
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = context =>
            {
                if (context.Principal?.Identity is ClaimsIdentity identity)
                {
                    foreach (var existingClaim in identity.FindAll("auth_source").ToList())
                    {
                        identity.RemoveClaim(existingClaim);
                    }

                    identity.AddClaim(new Claim("auth_source", "mcp"));
                }

                return Task.CompletedTask;
            }
        };
    });
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("FirebaseOnly", policy =>
    {
        policy.AddAuthenticationSchemes(AuthSchemes.FirebaseBearer);
        policy.RequireAuthenticatedUser();
    });

    options.AddPolicy("AdminOnly", policy =>
    {
        policy.AddAuthenticationSchemes(AuthSchemes.FirebaseBearer);
        policy.RequireAuthenticatedUser();
        policy.AddRequirements(new AdminEmailRequirement());
    });

    foreach (var scope in McpScopes.Supported)
    {
        options.AddPolicy(scope, policy =>
        {
            policy.RequireAuthenticatedUser();
            policy.AddRequirements(new McpScopeRequirement(scope));
        });
    }
});
builder.Services.AddScoped<IAuthorizationHandler, AdminEmailAuthorizationHandler>();
builder.Services.AddSingleton<IAuthorizationHandler, McpScopeAuthorizationHandler>();
builder.Services.AddRateLimiter(options =>
{
    options.AddPolicy("mcp-write", context =>
        RateLimitPartition.GetFixedWindowLimiter(GetUserOrIpPartition(context), _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 10,
            Window = TimeSpan.FromMinutes(10),
            QueueLimit = 0,
        }));

    options.AddPolicy("mcp-oauth", context =>
        RateLimitPartition.GetFixedWindowLimiter(GetIpPartition(context), _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 60,
            Window = TimeSpan.FromMinutes(10),
            QueueLimit = 0,
        }));

    options.AddPolicy("mcp-audit", context =>
        RateLimitPartition.GetFixedWindowLimiter(GetIpPartition(context), _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 120,
            Window = TimeSpan.FromMinutes(10),
            QueueLimit = 0,
        }));
});
builder.Services.AddHttpClient("mcp-client-metadata")
    .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
    {
        AllowAutoRedirect = false,
    });
builder.Services.AddSingleton<IMcpClientMetadataDnsResolver, DnsMcpClientMetadataResolver>();
builder.Services.AddScoped<McpClientMetadataValidator>();
builder.Services.AddScoped<McpOAuthService>();
builder.Services.AddScoped<McpTokenService>();
builder.Services.AddScoped<McpWriteIdempotencyService>();

// Firebase Admin SDK uses Application Default Credentials.
if (FirebaseApp.DefaultInstance is null)
{
    FirebaseApp.Create(new AppOptions
    {
        Credential = GoogleCredential.GetApplicationDefault(),
        ProjectId = builder.Configuration["Firebase:ProjectId"],
    });
}

// Application & Infrastructure layers
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// HTTP context + current user service
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<CurrentUserService>();
builder.Services.AddScoped<ICurrentUserService>(sp => sp.GetRequiredService<CurrentUserService>());

var app = builder.Build();

var migrateOnStartup = builder.Configuration.GetValue<bool>("Database:MigrateOnStartup");
if (app.Environment.IsDevelopment() || app.Environment.IsStaging() || migrateOnStartup)
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<ConvyDbContext>();
    await db.Database.MigrateAsync();
}

// Middleware pipeline
app.UseForwardedHeaders();
app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseRateLimiter();
app.UseAuthorization();
app.UseMiddleware<UserResolutionMiddleware>();

// Health check endpoints
app.MapHealthChecks("/health");
app.MapHealthChecks("/health/ready");

// Feature endpoints
app.MapUserEndpoints();
app.MapHouseholdEndpoints();
app.MapInviteEndpoints();
app.MapListEndpoints();
app.MapItemEndpoints();
app.MapTaskEndpoints();
app.MapActivityEndpoints();
app.MapDeviceEndpoints();
app.MapAdminEndpoints();
app.MapMcpOAuthEndpoints();
app.MapMcpAuditEndpoints();

// SignalR hub
app.MapHub<HouseholdHub>("/hubs/household");

app.Run();

static bool LooksLikeMcpAccessToken(string authorizationHeader)
{
    const string bearerPrefix = "Bearer ";
    if (!authorizationHeader.StartsWith(bearerPrefix, StringComparison.OrdinalIgnoreCase))
        return false;

    var token = authorizationHeader[bearerPrefix.Length..].Trim();
    var parts = token.Split('.');
    if (parts.Length < 2)
        return false;

    try
    {
        var payload = parts[1];
        var padding = payload.Length % 4;
        if (padding > 0)
            payload += new string('=', 4 - padding);

        var bytes = Convert.FromBase64String(payload.Replace('-', '+').Replace('_', '/'));
        using var document = JsonDocument.Parse(bytes);
        return document.RootElement.TryGetProperty("token_use", out var tokenUse)
               && string.Equals(tokenUse.GetString(), "mcp_access", StringComparison.Ordinal);
    }
    catch
    {
        return false;
    }
}

static string GetUserOrIpPartition(HttpContext context)
{
    var subject = context.User.FindFirst("sub")?.Value;
    return string.IsNullOrWhiteSpace(subject) ? GetIpPartition(context) : $"user:{subject}";
}

static string GetIpPartition(HttpContext context) =>
    $"ip:{context.Connection.RemoteIpAddress?.ToString() ?? "unknown"}";

// Make Program class accessible for WebApplicationFactory in tests
public partial class Program { }
