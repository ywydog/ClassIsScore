using ClassIsScore.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ClassIsScore.Tests;

public class XamlThemeServiceTests
{
    [Fact]
    public void Service_CanBeCreated()
    {
        var service = new XamlThemeService(NullLogger<XamlThemeService>.Instance);
        Assert.NotNull(service.Themes);
    }

    [Fact]
    public async Task LoadAllThemesAsync_DoesNotThrow()
    {
        var service = new XamlThemeService(NullLogger<XamlThemeService>.Instance);
        await service.LoadAllThemesAsync();
        Assert.Empty(service.Themes);
    }
}
