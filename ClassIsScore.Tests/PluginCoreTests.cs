using System;
using ClassIsScore.Models;
using ClassIsScore.Abstractions;
using ClassIsScore.Attributes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace ClassIsScore.Tests;

public class PluginCoreTests
{
    [PluginEntrance]
    public class TestPlugin : PluginBase
    {
        public override void Initialize(HostBuilderContext context, IServiceCollection services) {}
    }

    [Fact]
    public void PluginEntranceAttribute_CanBeApplied()
    {
        var attr = (PluginEntranceAttribute?)Attribute.GetCustomAttribute(typeof(TestPlugin), typeof(PluginEntranceAttribute));
        Assert.NotNull(attr);
    }
}
