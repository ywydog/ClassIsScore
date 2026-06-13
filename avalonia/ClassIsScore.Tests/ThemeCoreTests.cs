using ClassIsScore.Models;
using Xunit;

namespace ClassIsScore.Tests;

public class ThemeCoreTests
{
    [Fact]
    public void ThemeManifest_CanBeInstantiated()
    {
        var manifest = new ThemeManifest { Id = "test-theme", Name = "Test Theme" };
        Assert.Equal("test-theme", manifest.Id);
        Assert.Equal("Test Theme", manifest.Name);
    }

    [Fact]
    public void ThemeManifest_AllPropertiesWork()
    {
        var manifest = new ThemeManifest
        {
            Id = "dark-pro",
            Name = "Dark Pro",
            Version = "1.0.0",
            Author = "TestAuthor",
            Description = "A dark theme",
            TargetApiVersion = "1.0"
        };
        Assert.Equal("dark-pro", manifest.Id);
        Assert.Equal("1.0.0", manifest.Version);
        Assert.Equal("TestAuthor", manifest.Author);
    }

    [Fact]
    public void ThemeInfo_DefaultValues()
    {
        var info = new ThemeInfo();
        Assert.NotNull(info.Manifest);
        Assert.Equal(string.Empty, info.ThemeFolderPath);
        Assert.False(info.IsEnabled);
    }

    [Fact]
    public void ThemeInfo_CanSetProperties()
    {
        var info = new ThemeInfo
        {
            Manifest = new ThemeManifest { Id = "test" },
            ThemeFolderPath = "/data/Themes/test",
            IsEnabled = true
        };
        Assert.Equal("test", info.Manifest.Id);
        Assert.Equal("/data/Themes/test", info.ThemeFolderPath);
        Assert.True(info.IsEnabled);
    }
}
