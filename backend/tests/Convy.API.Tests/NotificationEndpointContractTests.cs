using FluentAssertions;

namespace Convy.API.Tests;

public class NotificationEndpointContractTests
{
    [Fact]
    public void UserEndpoints_ShouldExposeNotificationPreferenceRoutes()
    {
        var source = File.ReadAllText(Path.Combine(FindRepoRoot(), "backend", "src", "Convy.API", "Endpoints", "UserEndpoints.cs"));

        source.Should().Contain("notification-preferences");
        source.Should().Contain("GetNotificationPreferencesQuery");
        source.Should().Contain("UpdateNotificationPreferencesCommand");
    }

    [Fact]
    public void DeviceEndpoints_RegisterRequest_ShouldAcceptOptionalLocale()
    {
        var source = File.ReadAllText(Path.Combine(FindRepoRoot(), "backend", "src", "Convy.API", "Endpoints", "DeviceEndpoints.cs"));

        source.Should().Contain("string? Locale");
        source.Should().Contain("request.Locale");
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
