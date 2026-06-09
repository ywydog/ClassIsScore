using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ClassIsScore.Services.Abstractions;

/// <summary>
/// 应用主机接口，提供服务获取能力
/// </summary>
public interface IAppHost
{
    /// <summary>
    /// 获取指定类型的服务
    /// </summary>
    /// <typeparam name="T">要获取的服务类型</typeparam>
    /// <returns>获取到的服务实例</returns>
    /// <exception cref="InvalidOperationException">当服务未注册时抛出</exception>
    T GetService<T>();

    /// <summary>
    /// 尝试获取指定类型的服务
    /// </summary>
    /// <typeparam name="T">要获取的服务类型</typeparam>
    /// <returns>如果获取成功则返回服务实例，否则返回 null</returns>
    T? TryGetService<T>();
}
