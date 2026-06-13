using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ClassIsScore.Services.Abstractions;

namespace ClassIsScore.Services;

/// <summary>
/// 应用主机实现，持有 IHost 实例并提供服务获取能力
/// </summary>
public class AppHost : IAppHost
{
    private readonly IHost _host;

    /// <summary>
    /// 全局单例实例
    /// </summary>
    public static AppHost? Instance { get; set; }

    public AppHost(IHost host)
    {
        _host = host;
    }

    /// <summary>
    /// 获取指定类型的服务
    /// </summary>
    /// <typeparam name="T">要获取的服务类型</typeparam>
    /// <returns>获取到的服务实例</returns>
    /// <exception cref="InvalidOperationException">当服务未注册时抛出</exception>
    public T GetService<T>()
    {
        var service = _host.Services.GetService<T>();
        if (service == null)
        {
            throw new InvalidOperationException($"服务 {typeof(T).Name} 未注册");
        }
        return service;
    }

    /// <summary>
    /// 尝试获取指定类型的服务
    /// </summary>
    /// <typeparam name="T">要获取的服务类型</typeparam>
    /// <returns>如果获取成功则返回服务实例，否则返回 null</returns>
    public T? TryGetService<T>()
    {
        return _host.Services.GetService<T>();
    }
}
