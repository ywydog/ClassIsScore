using ClassIsScore.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ClassIsScore.Tests;

public class PluginServiceTests
{
    [Fact]
    public async Task LoadPluginsAsync_DoesNotThrow()
    {
        var service = new PluginService(NullLogger<PluginService>.Instance);
        await service.LoadPluginsAsync();
        Assert.NotNull(service.LoadedPlugins);
    }
}
