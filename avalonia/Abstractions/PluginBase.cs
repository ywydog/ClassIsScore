using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ClassIsScore.Models;

namespace ClassIsScore.Abstractions;

/// <summary>
/// 插件基类，所有插件必须继承此类
/// </summary>
public abstract class PluginBase
{
    /// <summary>
    /// 插件信息
    /// </summary>
    public PluginInfo Info { get; internal set; } = null!;

    /// <summary>
    /// 插件初始化，在Host构建阶段调用，用于注册服务
    /// </summary>
    /// <param name="context">Host构建上下文</param>
    /// <param name="services">服务集合</param>
    public abstract void Initialize(HostBuilderContext context, IServiceCollection services);
}
