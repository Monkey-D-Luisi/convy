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
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// Compose connection string from individual env vars (Cloud Run with Secret Manager)
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
builder.Services.AddHealthChecks();

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

// Authentication — Firebase JWT
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
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
    });
builder.Services.AddAuthorization();

// Firebase Admin SDK — uses Application Default Credentials
// (GOOGLE_APPLICATION_CREDENTIALS env var locally, metadata server on Cloud Run)
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

if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<ConvyDbContext>();
    await db.Database.MigrateAsync();
}

// Middleware pipeline
app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<UserResolutionMiddleware>();

// Health check endpoints
app.MapHealthChecks("/health");

// Feature endpoints
app.MapUserEndpoints();
app.MapHouseholdEndpoints();
app.MapInviteEndpoints();
app.MapListEndpoints();
app.MapItemEndpoints();
app.MapActivityEndpoints();
app.MapDeviceEndpoints();

// SignalR hub
app.MapHub<HouseholdHub>("/hubs/household");

app.Run();

// Make Program class accessible for WebApplicationFactory in tests
public partial class Program { }
