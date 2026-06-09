using System;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using ClassIsScore.Helpers;
using ClassIsScore.Models;
using ClassIsScore.Services.Abstractions;
using Microsoft.Extensions.Logging;

namespace ClassIsScore.Services;

/// <summary>
/// 应用状态服务实现，管理应用状态的持久化存储
/// </summary>
public class AppStateService : IAppStateService
{
    private readonly ILogger<AppStateService> _logger;
    private readonly string _stateFilePath;
    private AppState? _cachedState;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public AppStateService(ILogger<AppStateService> logger)
    {
        _logger = logger;
        _stateFilePath = Path.Combine(AppPaths.DataFolderPath, "app_state.json");
    }

    /// <summary>
    /// 获取应用状态
    /// </summary>
    /// <returns>当前应用状态</returns>
    public async Task<AppState> GetAppStateAsync()
    {
        if (_cachedState != null)
        {
            return _cachedState;
        }

        _cachedState = await LoadStateFromFileAsync();
        return _cachedState;
    }

    /// <summary>
    /// 保存应用状态
    /// </summary>
    /// <param name="state">要保存的应用状态</param>
    public async Task SaveAppStateAsync(AppState state)
    {
        _cachedState = state;

        try
        {
            var dir = Path.GetDirectoryName(_stateFilePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            var json = JsonSerializer.Serialize(state, JsonOptions);
            await File.WriteAllTextAsync(_stateFilePath, json);
            _logger.LogInformation("应用状态已保存");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "保存应用状态失败");
        }
    }

    /// <summary>
    /// 是否为首次启动
    /// </summary>
    /// <returns>如果是首次启动返回 true</returns>
    public async Task<bool> IsFirstLaunchAsync()
    {
        var state = await GetAppStateAsync();
        return !state.IsOnboardingCompleted;
    }

    /// <summary>
    /// 标记引导已完成
    /// </summary>
    public async Task MarkOnboardingCompletedAsync()
    {
        var state = await GetAppStateAsync();
        state.IsOnboardingCompleted = true;

        if (state.FirstLaunchDate == null)
        {
            state.FirstLaunchDate = DateTime.Now;
        }

        state.AppVersion = Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ?? "未知";

        await SaveAppStateAsync(state);
        _logger.LogInformation("引导已完成，标记已保存");
    }

    /// <summary>
    /// 从文件加载应用状态
    /// </summary>
    private async Task<AppState> LoadStateFromFileAsync()
    {
        try
        {
            if (File.Exists(_stateFilePath))
            {
                var json = await File.ReadAllTextAsync(_stateFilePath);
                var state = JsonSerializer.Deserialize<AppState>(json, JsonOptions);
                if (state != null)
                {
                    _logger.LogInformation("应用状态已加载");
                    return state;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "加载应用状态失败，使用默认状态");
        }

        // 返回默认状态（未完成引导）
        return new AppState
        {
            IsOnboardingCompleted = false,
            FirstLaunchDate = null,
            AppVersion = Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ?? "未知"
        };
    }
}
