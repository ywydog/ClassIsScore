using System.Threading.Tasks;
using ClassIsScore.Models;

namespace ClassIsScore.Services.Abstractions;

/// <summary>
/// 应用状态服务接口，管理应用的全局状态
/// </summary>
public interface IAppStateService
{
    /// <summary>
    /// 获取应用状态
    /// </summary>
    /// <returns>当前应用状态</returns>
    Task<AppState> GetAppStateAsync();

    /// <summary>
    /// 保存应用状态
    /// </summary>
    /// <param name="state">要保存的应用状态</param>
    Task SaveAppStateAsync(AppState state);

    /// <summary>
    /// 是否为首次启动
    /// </summary>
    /// <returns>如果是首次启动返回 true</returns>
    Task<bool> IsFirstLaunchAsync();

    /// <summary>
    /// 标记引导已完成
    /// </summary>
    Task MarkOnboardingCompletedAsync();
}
