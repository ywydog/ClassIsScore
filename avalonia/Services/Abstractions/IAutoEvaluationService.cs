using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ClassIsScore.Models;

namespace ClassIsScore.Services.Abstractions;

/// <summary>
/// 自动评价服务接口，提供定时自动评价的配置管理和执行功能
/// </summary>
public interface IAutoEvaluationService
{
    /// <summary>
    /// 自动评价执行事件
    /// </summary>
    event EventHandler<AutoEvaluationExecutedEventArgs>? EvaluationExecuted;

    /// <summary>
    /// 启动自动评价定时器
    /// </summary>
    Task StartAsync();

    /// <summary>
    /// 停止定时器
    /// </summary>
    Task StopAsync();

    /// <summary>
    /// 获取所有自动评价配置
    /// </summary>
    Task<List<AutoEvaluationConfig>> GetConfigsAsync();

    /// <summary>
    /// 添加自动评价配置
    /// </summary>
    /// <param name="config">配置对象</param>
    Task AddConfigAsync(AutoEvaluationConfig config);

    /// <summary>
    /// 更新自动评价配置
    /// </summary>
    /// <param name="config">配置对象</param>
    Task UpdateConfigAsync(AutoEvaluationConfig config);

    /// <summary>
    /// 删除自动评价配置
    /// </summary>
    /// <param name="id">配置ID</param>
    Task DeleteConfigAsync(Guid id);
}
