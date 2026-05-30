using FluentAssertions;

namespace Convy.API.Tests;

public class ConfigurationHygieneTests
{
    [Fact]
    public void AppsettingsJson_ShouldNotCommitDatabaseCredentials()
    {
        var appsettingsPath = Path.Combine(FindRepoRoot(), "backend", "src", "Convy.API", "appsettings.json");
        var appsettings = File.ReadAllText(appsettingsPath);

        appsettings.Should().NotContain("convy_dev_password");
        appsettings.Should().NotContain("Username=convy");
        appsettings.Should().NotContain("Password=");
    }

    [Fact]
    public void Program_ShouldOnlyApplyMigrationsInDevelopment()
    {
        var programPath = Path.Combine(FindRepoRoot(), "backend", "src", "Convy.API", "Program.cs");
        var program = File.ReadAllText(programPath);

        var environmentGuardIndex = program.IndexOf("app.Environment.IsDevelopment()", StringComparison.Ordinal);
        var migrationIndex = program.IndexOf("MigrateAsync", StringComparison.Ordinal);

        environmentGuardIndex.Should().BeGreaterThanOrEqualTo(0);
        migrationIndex.Should().BeGreaterThanOrEqualTo(0);
        environmentGuardIndex.Should().BeLessThan(migrationIndex);
    }

    [Fact]
    public void Program_ShouldFailFastForMissingMcpPublicKeyOutsideDevelopment()
    {
        var programPath = Path.Combine(FindRepoRoot(), "backend", "src", "Convy.API", "Program.cs");
        var program = File.ReadAllText(programPath);

        program.Should().Contain("MCP public key is required outside Development.");
        program.Should().NotContain("?? new RsaSecurityKey(RSA.Create(2048))");
    }

    [Fact]
    public void Infrastructure_ShouldRequireOpenAiKeyWhenVoiceParsingEnabledOutsideDevelopment()
    {
        var source = File.ReadAllText(Path.Combine(FindRepoRoot(), "backend", "src", "Convy.Infrastructure", "DependencyInjection.cs"));

        source.Should().Contain("Features:VoiceParsingEnabled");
        source.Should().Contain("if (!voiceParsingEnabled)");
        source.Should().Contain("OpenAI API key is required when voice parsing is enabled outside Development.");
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
