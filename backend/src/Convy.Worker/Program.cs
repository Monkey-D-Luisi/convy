using Convy.Worker;
using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);

WorkerServiceRegistration.ConfigureServices(builder.Services, builder.Configuration);
InitializeFirebase(builder.Configuration, builder.Environment);

var app = builder.Build();
await app.RunAsync();

static void InitializeFirebase(IConfiguration configuration, IHostEnvironment environment)
{
    if (FirebaseApp.DefaultInstance is not null)
        return;

    var projectId = configuration["Firebase:ProjectId"];
    if (string.IsNullOrWhiteSpace(projectId) && environment.IsDevelopment())
        return;

    if (string.IsNullOrWhiteSpace(projectId))
        throw new InvalidOperationException("Firebase project ID is required for the worker outside Development.");

    FirebaseApp.Create(new AppOptions
    {
        Credential = GoogleCredential.GetApplicationDefault(),
        ProjectId = projectId,
    });
}
