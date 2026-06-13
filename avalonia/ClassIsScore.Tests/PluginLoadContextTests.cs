using ClassIsScore.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ClassIsScore.Tests;

public class PluginLoadContextTests
{
    [Fact]
    public void Context_CanBeCreated()
    {
        // 使用当前程序集路径创建，仅验证构造函数不抛异常
        var path = typeof(PluginLoadContextTests).Assembly.Location;
        var context = new PluginLoadContext(path);
        Assert.NotNull(context);
    }
}
